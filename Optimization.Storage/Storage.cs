/*
 *  Storage.cs - This file is part of optimization-sharp
 *
 *  Copyright (C) 2009 - Jesse van den Kieboom
 *
 * This library is free software; you can redistribute it and/or modify it
 * under the terms of the GNU Lesser General Public License as published by the
 * Free Software Foundation; either version 2.1 of the License, or (at your
 * option) any later version.
 *
 * This library is distributed in the hope that it will be useful, but WITHOUT
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License
 * for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this library; if not, write to the Free Software Foundation,
 * Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
 */
using System;
using System.Data;
using System.Data.Common;
using Mono.Data.Sqlite;
using System.Reflection;
using System.Xml.Serialization;
using System.Collections.Generic;
using Biorob.Math;
using System.Text;
using System.IO;
using System.Collections;

namespace Optimization.Storage
{
	public class Storage : Database
	{
		private struct LogItem
		{
			public long TimeStamp;
			public string Type;
			public string Message;
			
			public LogItem(long timestamp, string type, string message)
			{
				TimeStamp = timestamp;
				Type = type;
				Message = message;
			}
		}

		private Job d_job;
		private List<LogItem> d_logBatch;

		public Storage(Job job)
		{
			d_job = job;
			d_logBatch = new List<LogItem>();
		}

		public Job Job
		{
			get
			{
				return d_job;
			}
		}

		public void Begin()
		{
			if (String.IsNullOrEmpty(Uri))
			{
				return;
			}

			Uri = UniqueFile(Uri);

			Open();
		}

		public new bool Open()
		{
			bool exists = File.Exists(Uri);
			
			base.Open();

			if (!d_job.SynchronousStorage)
			{
				Query("PRAGMA synchronous = OFF");
			}

			if (!exists)
			{
				CreateTables();
			}

			/* Check if it's valid */
			bool ret = !exists || QueryFirst("pragma table_info(job)") != null;

			if (!ret)
			{
				Close();
			}
		
			if (exists && ret)
			{
				Upgrade();
			}

			return ret;
		}
		
		private void Upgrade()
		{
			bool hasfilename = false;

			Query("PRAGMA table_info(job)", delegate (IDataReader reader) {
				string name = (string)reader[1];
				
				if (name == "filename")
				{
					hasfilename = true;
					return false;
				}
				
				return true;
			});
			
			if (!hasfilename)
			{
				Query("ALTER TABLE `job` ADD COLUMN `filename` TEXT");
			}
			
			Dictionary<string, string> vals = new Dictionary<string, string>();
			
			Query("PRAGMA table_info(state)", delegate (IDataReader reader) {
				string name = (string)reader[1];
				
				if (name != null && name.StartsWith("_s_"))
				{
					vals[name.Substring(3)] = name;
				}
				
				return true;
			});
			
			if (!vals.ContainsKey("initial-population"))
			{
				Query(String.Format("ALTER TABLE `state` ADD COLUMN `_s_initial-population` TEXT"));
				Query("UPDATE `state` SET `_s_initial-population` = ''");
			}
			
			if (!vals.ContainsKey("initial-population-noise"))
			{
				Query("ALTER TABLE `state` ADD COLUMN `_s_initial-population-noise` TEXT");
				Query("UPDATE `state` SET `_s_initial-population-noise` = '0'");
			}
			
			vals.Clear();
			
			Query("SELECT `name` FROM settings", delegate (IDataReader reader) {
				string name = reader[0] as string;
				vals[name] = name;
				return true;
			});
			
			if (!vals.ContainsKey("initial-population"))
			{
				Query("INSERT INTO `settings` (`name`, `value`) VALUES(@0, @1)", "initial-population", "");
				Query("INSERT INTO `settings` (`name`, `value`) VALUES(@0, @1)", "initial-population-noise", "0");
			}
		}

		private string UniqueFile(string filename)
		{
			string orig = Path.GetFullPath(filename);
			int i = 1;

			string ext = Path.GetExtension(orig);
			orig = orig.Substring(0, orig.Length - ext.Length);

			while (true)
			{
				string unique = orig + "." + i + ext;

				if (!File.Exists(unique))
				{
					return unique;
				}

				++i;
			}
		}

		public void End()
		{
			if (d_logBatch.Count != 0)
			{
				DbTransaction transaction = BeginTransaction();
				SaveLog();
				transaction.Commit();
			}

			Close();
		}

		private string NormalizeName(string name)
		{
			return name.Replace("`", "").Replace("'", "").Replace("\"", "");
		}

		private void InitializeFitnessTable()
		{
			if (Job.Optimizer.Population.Count == 0)
			{
				return;
			}

			Solution solution = Job.Optimizer.Population[0];
			StringBuilder builder = new StringBuilder();

			Query("DROP INDEX IF EXISTS fitness_index");
			Query("DROP INDEX IF EXISTS fitness_iteration");
			Query("DROP TABLE IF EXISTS `fitness`");

			builder.Append("CREATE TABLE `fitness` (`iteration` INT, `index` INT, `value` DOUBLE");

			foreach (KeyValuePair<string, double> pair in solution.Fitness.Values)
			{
				string norm = NormalizeName(pair.Key);

				builder.AppendFormat(", `_f_{0}`", norm);
			}

			foreach (KeyValuePair<string, Fitness.Variable> pair in solution.Fitness.Variables)
			{
				string norm = NormalizeName(pair.Key);

				builder.AppendFormat(", `_fv_{0}`", norm);
			}

			builder.Append(")");

			Query(builder.ToString());

			Query("CREATE INDEX fitness_index ON fitness(`index`)");
			Query("CREATE INDEX fitness_iteration ON fitness(`iteration`)");
			Query("CREATE INDEX IF NOT EXISTS solution_fitness ON solution(`fitness`)");
		}
		
		private void InitializeParametersTable()
		{
			InitializeParametersTable("parameter_values");
		}
		
		private void InitializeParametersActiveTable()
		{
			InitializeParametersTable("parameter_active");
		}

		private void InitializeParametersTable(string name)
		{
			if (Job.Optimizer.Population.Count == 0)
			{
				return;
			}

			StringBuilder builder = new StringBuilder();

			Query("DROP INDEX IF EXISTS " + name + "_index");
			Query("DROP INDEX IF EXISTS " + name + "_iteration");
			Query("DROP TABLE IF EXISTS `" + name + "`");
			
			builder.Append("CREATE TABLE `" + name + "` (`iteration` INT, `index` INT");

			foreach (Parameter parameter in Job.Optimizer.Parameters)
			{
				string norm = NormalizeName(parameter.Name);
				builder.AppendFormat(", `_p_{0}` DOUBLE DEFAULT 0", norm);
			}

			builder.Append(")");

			Query(builder.ToString());

			Query("CREATE INDEX " + name + "_index ON " + name + "(`index`)");
			Query("CREATE INDEX " + name + "_iteration ON " + name + "(`iteration`)");
		}

		private void InitializeDataTable()
		{
			if (Job.Optimizer.Population.Count == 0)
			{
				return;
			}

			Solution solution = Job.Optimizer.Population[0];
			StringBuilder builder = new StringBuilder();

			Query("DROP INDEX IF EXISTS data_index");
			Query("DROP INDEX IF EXISTS data_iteration");
			Query("DROP TABLE IF EXISTS `data`");

			builder.Append("CREATE TABLE `data` (`iteration` INT, `index` INT");

			foreach (KeyValuePair<string, object> data in solution.Data)
			{
				string norm = NormalizeName(data.Key);
				builder.AppendFormat(", `_d_{0}`", norm);
			}

			builder.Append(")");

			Query(builder.ToString());

			Query("CREATE INDEX data_index ON data(`index`)");
			Query("CREATE INDEX data_iteration ON data(`iteration`)");
		}

		private void InitializeStateTable()
		{
			Query("DROP TABLE IF EXISTS `state`");

			StringBuilder builder = new StringBuilder();
			builder.Append("CREATE TABLE IF NOT EXISTS `state` (`iteration` INT, `random` BLOB");

			foreach (KeyValuePair<string, object> pair in Job.Optimizer.State.Settings)
			{
				builder.AppendFormat(", `_s_{0}`", NormalizeName(pair.Key));
			}

			builder.Append(")");
			Query(builder.ToString());
			Query("CREATE INDEX state_iteration ON state(`iteration`)");
		}

		private void InitializeFirst()
		{
			DbTransaction transaction = BeginTransaction();

			InitializeFitnessTable();
			InitializeParametersTable();
			InitializeParametersActiveTable();
			InitializeDataTable();
			InitializeStateTable();

			transaction.Commit();
		}

		delegate bool GenerateSavePair(object container, out string name, out object val);

		private void SavePairs(Solution solution, ICollection collection, string table, Dictionary<string, object> additional, GenerateSavePair generator)
		{
			IDbCommand cmd = null;
			SavePairs(solution, collection, table, additional, generator, ref cmd);

			cmd.Dispose();
			cmd = null;
		}

		private void SavePairs(Solution solution, ICollection collection, string table, Dictionary<string, object> additional, GenerateSavePair generator, ref IDbCommand cmd)
		{
			List<object> vals = new List<object>();
			StringBuilder builder = null;
			StringBuilder values = null;

			if (cmd == null)
			{
				builder = new StringBuilder();
				builder.AppendFormat("INSERT INTO `{0}` (`iteration`", table);

				values = new StringBuilder();
				values.Append("VALUES(@0");
			}

			vals.Add(Job.Optimizer.CurrentIteration);

			int i = 1;

			if (solution != null)
			{
				if (cmd == null)
				{
					builder.Append(", `index`");
					values.Append(", @1");
				}

				vals.Add(solution.Id);
				++i;
			}

			if (additional != null)
			{
				foreach (KeyValuePair<string, object> pair in additional)
				{
					if (cmd == null)
					{
						builder.AppendFormat(", `{0}`", pair.Key);
						values.AppendFormat(", @{0}", i);
					}

					vals.Add(pair.Value);
					++i;
				}
			}

			foreach (object o in collection)
			{
				string name;
				object val;

				if (generator(o, out name, out val))
				{
					if (cmd == null)
					{
						builder.AppendFormat(", `{0}`", NormalizeName(name));
						values.AppendFormat(", @{0}", i);
					}

					vals.Add(val);
					++i;
				}
			}

			string q = null;
			var v = vals.ToArray();

			if (cmd == null)
			{
				builder.Append(") ").Append(values).Append(")");
				q = builder.ToString();
				Query(ref cmd, q, v);
			}
			else
			{
				Query(ref cmd, v);
			}
		}

		private void SaveFitness(Solution solution)
		{
			Dictionary<string, object> additional = new Dictionary<string, object>();
			additional["value"] = solution.Fitness.Value;
			
			Dictionary<string, double> fitvalues = new Dictionary<string, double>();
			
			foreach (KeyValuePair<string, double> pair in solution.Fitness.Values)
			{
				fitvalues["_f_" + pair.Key] = pair.Value;
			}
			
			foreach (KeyValuePair<string, Optimization.Fitness.Variable> pair in solution.Fitness.Variables)
			{
				fitvalues["_fv_" + pair.Key] = pair.Value.Expression.Evaluate(Biorob.Math.Constants.Context, solution.Fitness.Context);
			}

			SavePairs(solution, fitvalues, "fitness", additional, delegate (object o, out string name, out object val) {
				KeyValuePair<string, double> pair = (KeyValuePair<string, double>)o;

				name = pair.Key;
				val = pair.Value;

				return true;
			});
		}

		private void SaveParameters(Solution solution, ref IDbCommand cmd)
		{
			SavePairs(solution, solution.Parameters, "parameter_values", null, delegate (object o, out string name, out object val) {
				Parameter parameter = (Parameter)o;

				name = "_p_" + parameter.Name;
				val = parameter.Value;

				return true;
			}, ref cmd);
		}
		
		private void SaveActiveParameters(Solution solution)
		{
			List<object> values = new List<object>();
			StringBuilder q = new StringBuilder();
			StringBuilder valq = new StringBuilder();
			
			q.Append("INSERT INTO `parameter_active` (`iteration`, `index`");
			valq.Append("@0, @1");

			values.Add(Job.Optimizer.CurrentIteration);
			values.Add(solution.Id);
			
			for (int i = 0; i < solution.Parameters.Count; ++i)
			{
				q.Append(", ");
				valq.Append(", ");
				
				q.AppendFormat("`_p_{0}`", NormalizeName(solution.Parameters[i].Name));
				valq.AppendFormat("@{0}", i + 2);
				
				values.Add(1);
			}
			
			q.AppendFormat(") VALUES (").Append(valq).Append(")");
			
			Query(q.ToString(), values.ToArray());
		}
		
		public void SaveActiveParameters()
		{
			DbTransaction transaction = BeginTransaction();
			
			foreach (Solution solution in Job.Optimizer.Population)
			{
				SaveActiveParameters(solution);
			}
			
			transaction.Commit();
		}

		private void SaveData(Solution solution, ref IDbCommand cmd)
		{
			SavePairs(solution, solution.Data, "data", null, delegate (object o, out string name, out object val) {
				KeyValuePair<string, object> data = (KeyValuePair<string, object>)o;

				name = "_d_" + data.Key;
				val = data.Value;

				return true;
			}, ref cmd);
		}

		private void SaveState()
		{
			Dictionary<string, object> additional = new Dictionary<string, object>();
			System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter;

			formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

			MemoryStream stream = new MemoryStream();
			formatter.Serialize(stream, Job.Optimizer.State.Random);

			additional["random"] = stream.ToArray();
			stream.Close();

			SavePairs(null, Job.Optimizer.State.Settings.All(), "state", additional, delegate (object o, out string name, out object val) {
				KeyValuePair<string, object> data = (KeyValuePair<string, object>)o;

				name = "_s_" + data.Key;
				val = data.Value;

				return true;
			});
		}

		private void Save(Solution solution, List<IDbCommand> cmds)
		{
			Query("INSERT INTO `solution` (`index`, `iteration`, `fitness`) VALUES (@0, @1, @2)",
			      solution.Id, Job.Optimizer.CurrentIteration, solution.Fitness.Value);

			SaveFitness(solution);

			bool first = false;

			if (cmds.Count == 0)
			{
				cmds.Add(null);
				cmds.Add(null);

				first = true;
			}

			IDbCommand cmdParameters = cmds[0];
			IDbCommand cmdData = cmds[1];

			SaveParameters(solution, ref cmdParameters);
			SaveData(solution, ref cmdData);

			if (first)
			{
				cmds[0] = cmdParameters;
				cmds[1] = cmdData;
			}
		}
		
		private void SaveLog()
		{
			foreach (LogItem item in d_logBatch)
			{
				Query("INSERT INTO `log` (`time`, `type`, `message`) VALUES (@0, @1, @2)", item.TimeStamp, item.Type, item.Message);
			}
			
			d_logBatch.Clear();
		}

		public void SaveIteration()
		{
			if (Job.Optimizer.CurrentIteration == 0)
			{
				InitializeFirst();
			}

			DbTransaction transaction = BeginTransaction();

			int bestid = -1;
			double bestfitness = 0;

			if (Job.Optimizer.Best != null)
			{
				bestid = (int)Job.Optimizer.Best.Id;
				bestfitness = Job.Optimizer.Best.Fitness.Value;
			}

			Query("INSERT INTO `iteration` (`iteration`, `best_id`, `best_fitness`, `time`) VALUES(@0, @1, @2, @3)",
			      Job.Optimizer.CurrentIteration, bestid, bestfitness, UnixTimeStamp);

			List<IDbCommand> cmds = new List<IDbCommand>();

			foreach (Solution solution in Job.Optimizer)
			{
				Save(solution, cmds);
			}

			SaveState();
			SaveLog();

			transaction.Commit();
		}

		public void SaveToken()
		{
			if (!this)
			{
				return;
			}

			Query("UPDATE job SET token = @0", Job.Token);
		}

		public void SaveSettings()
		{
			DbTransaction transaction = BeginTransaction();

			Query("CREATE TABLE IF NOT EXISTS `job` (`filename` TEXT, `name` TEXT, `optimizer` TEXT, `dispatcher` TEXT, `priority` DOUBLE, `timeout` DOUBLE, `token` TEXT)");
			Query("DELETE FROM `job`");

			Query("INSERT INTO `job` (`filename`, `name`, `optimizer`, `dispatcher`, `priority`, `timeout`, `token`) VALUES(@0, @1, @2, @3, @4, @5, @6)", Job.Filename, Job.Name, Job.Optimizer.Name, Job.Dispatcher.Name, Job.Priority, Job.Timeout, Job.Token);

			Query("CREATE TABLE IF NOT EXISTS `settings` (`id` INTEGER PRIMARY KEY AUTOINCREMENT, `name` TEXT, `value` TEXT)");
			Query("DELETE FROM `settings`");

			Settings settings = Job.Optimizer.Configuration;

			// Settings
			foreach (KeyValuePair<string, object> pair in settings)
			{
				Query("INSERT INTO `settings` (`name`, `value`) VALUES(@0, @1)", pair.Key, pair.Value != null ? pair.Value.ToString() : null);
			}

			Query("CREATE TABLE IF NOT EXISTS `fitness_settings` (`id` INTEGER PRIMARY KEY AUTOINCREMENT, `name` TEXT, `value` TEXT, `mode` TEXT)");

			Query("DELETE FROM `fitness_settings`");
			Query("INSERT INTO `fitness_settings` (`name`, `value`) VALUES (@0, @1)", "__expression__", Job.Optimizer.Fitness.Expression.Text);
			Query("INSERT INTO `fitness_settings` (`name`, `value`) VALUES (@0, @1)", "__mode__", Fitness.ModeAsString(Fitness.CompareMode));

			foreach (KeyValuePair<string, Fitness.Variable> var in Job.Optimizer.Fitness.Variables)
			{
				Query("INSERT INTO `fitness_settings` (`name`, `value`, `mode`) VALUES(@0, @1, @2)", var.Key, var.Value.Expression.Text, Fitness.ModeAsString(var.Value.Mode));
			}

			Query("CREATE TABLE IF NOT EXISTS `boundaries` (`id` INTEGER PRIMARY KEY AUTOINCREMENT, `name` TEXT, `min` DOUBLE, `max` DOUBLE, `min_initial` DOUBLE, `max_initial` DOUBLE, `min_repr` TEXT, `max_repr` TEXT, `min_initial_repr` TEXT, `max_initial_repr` TEXT)");
			Query("DELETE FROM `boundaries`");

			foreach (Boundary boundary in Job.Optimizer.Boundaries)
			{
				Query(@"INSERT INTO `boundaries` (`name`, `min`, `max`, `min_initial`, `max_initial`,
				                                  `min_repr`, `max_repr`, `min_initial_repr`, `max_initial_repr`)
				        VALUES (@0, @1, @2, @3, @4, @5, @6, @7, @8)",
					boundary.Name,
					boundary.Min,
					boundary.Max,
					boundary.MinInitial,
					boundary.MaxInitial,
					boundary.MinSetting.Representation,
					boundary.MaxSetting.Representation,
					boundary.MinInitialSetting.Representation,
					boundary.MaxInitialSetting.Representation);
			}

			Query("CREATE TABLE IF NOT EXISTS `parameters` (`id` INTEGER PRIMARY KEY AUTOINCREMENT, `name` TEXT, `boundary` TEXT)");
			Query("DELETE FROM parameters");

			foreach (Parameter parameter in Job.Optimizer.Parameters)
			{
				Query("INSERT INTO `parameters` (`name`, `boundary`) VALUES (@0, @1)", parameter.Name, parameter.Boundary.Name);
			}

			Query("CREATE TABLE IF NOT EXISTS `dispatcher` (`id` INTEGER PRIMARY KEY AUTOINCREMENT, `name` TEXT, `value` TEXT)");
			Query("DELETE FROM `dispatcher`");

			foreach (KeyValuePair<string, string> disp in Job.Dispatcher.Settings)
			{
				Query("INSERT INTO `dispatcher` (`name`, `value`) VALUES (@0, @1)", disp.Key, disp.Value);
			}

			Query("CREATE TABLE IF NOT EXISTS `extensions` (`id` INTEGER PRIMARY KEY AUTOINCREMENT, `name` TEXT)");
			Query("DELETE FROM `extensions`");

			foreach (Extension ext in Job.Optimizer.Extensions)
			{
				Query("INSERT INTO `extensions` (`name`) VALUES (@0)", Extension.GetName(ext.GetType()));
			}

			transaction.Commit();
		}

		private void CreateTables()
		{
			DbTransaction transaction = BeginTransaction();

			Query("CREATE TABLE IF NOT EXISTS `iteration` (`iteration` INT PRIMARY KEY, `best_id` INT, `best_fitness` DOUBLE, `time` INT)");
			Query("CREATE INDEX IF NOT EXISTS iteration_iteration ON iteration(`iteration`)");

			Query("CREATE TABLE IF NOT EXISTS `solution` (`index` INT, `iteration` INT REFERENCES `iteration` (`iteration`), `fitness` DOUBLE)");
			Query("CREATE INDEX IF NOT EXISTS solution_index ON solution(`index`)");
			Query("CREATE INDEX IF NOT EXISTS solution_iteration ON solution(`iteration`)");

			Query("CREATE TABLE IF NOT EXISTS `log` (`id` INTEGER PRIMARY KEY AUTOINCREMENT, `time` INT, `type` TEXT, `message` TEXT)");
			Query("DELETE FROM `log`");

			transaction.Commit();

			SaveSettings();
		}

		public long UnixTimeStamp
		{
			get
			{
				return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
			}
		}

		public void Log(string type, string str)
		{
			d_logBatch.Add(new LogItem(UnixTimeStamp, type, str));
		}

		/* Data retrieval */
		private Records.Solution CreateSolution(IDataReader reader)
		{
			Records.Solution solution = new Records.Solution();

			solution.Index = As<int>(reader["the_index"]);
			solution.Iteration = As<int>(reader["the_iteration"]);

			for (int i = 0; i < reader.FieldCount; ++i)
			{
				string name = reader.GetName(i);
				
				object val = reader.GetValue(i);

				if (name.StartsWith("_f_"))
				{
					solution.Fitness.Add(name.Substring(3), As<double>(val));
				}
				else if (name.StartsWith("_p_"))
				{
					solution.Parameters.Add(name.Substring(3), As<double>(val));
				}
				else if (name.StartsWith("_d_"))
				{
					solution.Data.Add(name.Substring(3), As<string>(val));
				}
			}

			solution.FitnessValue = As<double>(reader["fitness"]);
			return solution;
		}

		public Records.Iteration ReadIteration(int iteration)
		{
			Records.Iteration ret = new Records.Iteration();

			object[] vals = QueryFirst("SELECT `best_id`, `time` FROM iteration WHERE `iteration` = @0", iteration);

			int bestId = As<int>(vals[0]);

			ret.Index = iteration;
			ret.Time = FromUnixTimestamp(As<int>(vals[1]));

			Dictionary<int, Records.Solution> idmap = new Dictionary<int, Records.Solution>();

			Query(@"SELECT solution.iteration AS `the_iteration`, solution.`index` AS `the_index`, solution.*, fitness.*, parameter_values.*, data.* FROM solution
                    LEFT JOIN fitness ON (fitness.`index` = solution.`index` AND
                                          fitness.`iteration` = solution.`iteration`)
                    LEFT JOIN parameter_values ON (`parameter_values`.`index` = solution.`index` AND
                                            `parameter_values`.`iteration` = solution.`iteration`)
                    LEFT JOIN data ON (data.`index` = solution.`index` AND
                                       data.`iteration` = solution.`iteration`)
                    WHERE `solution`.`iteration` = @0", delegate (IDataReader reader) {

				Records.Solution sol = CreateSolution(reader);

				idmap[sol.Index] = sol;
				ret.Solutions.Add(sol);
				return true;
			}, iteration);

			if (idmap.ContainsKey(bestId))
			{
				ret.Best = idmap[bestId];
			}

			return ret;
		}

		public Records.Solution ReadSolution(int iteration, int id)
		{
			Records.Solution solution = null;
			string condition = "";

			if (iteration >= 0 && id >= 0)
			{
				condition = "`solution`.`iteration` = @0 AND `solution`.`index` = @1";
			}
			else if (iteration >= 0)
			{
				condition = "`solution`.`iteration` = @0";
			}
			else if (id >= 0)
			{
				condition = "`solution`.`index` = @0";
				iteration = id;
			}

			if (condition != "")
			{
				condition = "WHERE " + condition;
			}

			string q = String.Format(@"SELECT solution.iteration AS the_iteration, solution.`index` AS the_index, solution.*, fitness.*, parameter_values.*, data.* FROM solution
                    LEFT JOIN fitness ON (fitness.`index` = solution.`index` AND
                                          fitness.`iteration` = solution.`iteration`)
                    LEFT JOIN parameter_values ON (parameter_values.`index` = solution.`index` AND
                                             parameter_values.`iteration` = solution.`iteration`)
                    LEFT JOIN data ON (data.`index` = solution.`index` AND
                                       data.`iteration` = solution.`iteration`)
                    {0} ORDER BY solution.fitness DESC LIMIT 1", condition);

			Query(q, delegate (IDataReader reader) {
				solution = CreateSolution(reader);
				return false;
			}, iteration, id);

			return solution;
		}

		public static T As<T>(object val, T def = default(T))
		{
			if (val == null)
			{
				return def;
			}

			try
			{
				return (T)Convert.ChangeType(val, typeof(T));
			}
			catch
			{
				return def;
			}
		}

		public Records.Job ReadJob()
		{
			Records.Job job = new Records.Job();
			object[] jobspec = QueryFirst("SELECT `name`, `priority`, `timeout`, `token`, `optimizer`, `dispatcher`, `filename` FROM `job`");

			job.Name = As<string>(jobspec[0]);
			job.Priority = As<double>(jobspec[1], 1);
			job.Timeout = As<double>(jobspec[2]);
			job.Token = As<string>(jobspec[3]);
			job.Optimizer.Name = As<string>(jobspec[4]);
			job.Dispatcher.Name = As<string>(jobspec[5]);
			job.Filename = As<string>(jobspec[6]);
			
			/* Optimizer stuff */
			Query("SELECT `name`, `value` FROM `settings`", delegate (IDataReader reader) {
				job.Optimizer.Settings[As<string>(reader[0])] = As<string>(reader[1]);
				return true;
			});

			Dictionary<string, Records.Boundary> boundaries = new Dictionary<string, Records.Boundary>();

			Query(@"SELECT `name`, `min_repr`, `max_repr`, `min_initial_repr`, `max_initial_repr`
			        FROM `boundaries`", delegate (IDataReader reader) {
				Records.Boundary boundary = new Records.Boundary();

				boundary.Name = As<string>(reader[0]);
				boundary.Min = As<string>(reader[1]);
				boundary.Max = As<string>(reader[2]);
				boundary.MinInitial = As<string>(reader[3]);
				boundary.MaxInitial = As<string>(reader[4]);

				job.Optimizer.Boundaries.Add(boundary);
				boundaries[boundary.Name] = boundary;

				return true;
			});

			Query("SELECT `name`, `boundary` FROM `parameters`", delegate (IDataReader reader) {
				Records.Parameter parameter = new Records.Parameter();
				parameter.Name = As<string>(reader[0]);
				parameter.Boundary = boundaries[As<string>(reader[1])];

				job.Optimizer.Parameters.Add(parameter);
				return true;
			});

			/* Dispatcher stuff */
			Query("SELECT `name`, `value` FROM `dispatcher`", delegate (IDataReader reader) {
				job.Dispatcher.Settings[As<string>(reader[0])] = As<string>(reader[1]);
				return true;
			});

			/* Fitness stuff */
			Query("SELECT `name`, `value`, `mode` FROM `fitness_settings`", delegate (IDataReader reader) {
				string name = As<string>(reader[0]);
				string val = As<string>(reader[1]);
				string mode = As<string>(reader[2]);

				if (name == "__expression__")
				{
					job.Optimizer.Fitness.Expression = val;
				}
				else if (name == "__mode__")
				{
					job.Optimizer.Fitness.Mode = val;
				}
				else
				{
					job.Optimizer.Fitness.Variables.Add(name, new Records.Fitness.Variable(val, mode));
				}

				return true;
			});

			/* State stuff */
			Int64 iteration = As<Int64>(QueryValue("SELECT COUNT(iteration) FROM iteration"));

			if (iteration > 0)
			{
				Query("SELECT * FROM `state` ORDER BY iteration DESC LIMIT 1", delegate (IDataReader reader) {
					byte[] random = (byte[])reader["random"];

					System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter;
					formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

					MemoryStream stream = new MemoryStream();
					stream.Write(random, 0, random.Length);
					stream.Seek(0, SeekOrigin.Begin);

					job.Optimizer.State.Random = (Optimization.Random)formatter.Deserialize(stream);
					stream.Close();

					for (int i = 0; i < reader.FieldCount; ++i)
					{
						string name = reader.GetName(i);

						if (name.StartsWith("_s_"))
						{
							job.Optimizer.State.Settings.Add(name.Substring(3), As<string>(reader[i]));
						}
					}

					return false;
				});
			}

			/* Extensions stuff */
			Query("SELECT `name` FROM `extensions`", delegate (IDataReader reader) {
				string name = As<string>(reader[0]);

				job.Extensions.Add(name);
				return true;
			});

			return job;
		}

		public long ReadIterations()
		{
			return As<long>(QueryValue("SELECT COUNT(`iteration`) FROM iteration"));
		}

		public long ReadSolutions(long iteration)
		{
			return As<long>(QueryValue("SELECT COUNT(*) FROM solution WHERE iteration = @0", iteration));
		}

		private DateTime FromUnixTimestamp(int seconds)
		{
			DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
			return epoch.AddSeconds((double)seconds);
		}

		public List<Records.Log> ReadLog()
		{
			List<Records.Log> ret = new List<Records.Log>();

			Query("SELECT * FROM `log`", delegate (IDataReader reader) {
				Records.Log log = new Records.Log();

				log.Time = FromUnixTimestamp(As<int>(reader["time"]));
				log.Type = As<string>(reader["type"]);
				log.Message = As<string>(reader["message"]);

				ret.Add(log);
				return true;
			});

			return ret;
		}
		
		public Records.InitialPopulation ReadInitialPopulation()
		{
			List<string> parameters = new List<string>();
			
			Query("PRAGMA table_info(`initial_population`)", delegate (IDataReader reader) {
				string name = As<string>(reader[1]);
				
				if (name.StartsWith("_p_"))
				{
					parameters.Add(name);
				}
				
				return true;
			});
			
			List<string> data = new List<string>();
			
			Query("PRAGMA table_info(`initial_population_data`)", delegate (IDataReader reader) {
				string name = As<string>(reader[1]);
				
				if (name.StartsWith("_d_"))
				{
					data.Add(name);
				}
				
				return true;
			});
			
			if (parameters.Count == 0)
			{
				return null;
			}

			Records.InitialPopulation ret = new Records.InitialPopulation();

			string paramsel = String.Join(", ", Array.ConvertAll(parameters.ToArray(), a => String.Format("`initial_population`.`{0}`", a)));
			string datasel = String.Join(", ", Array.ConvertAll(data.ToArray(), a => String.Format("`initial_population_data`.`{0}`", a)));

			Query("SELECT " + paramsel + ", " + datasel + " FROM `initial_population` " +
			       "LEFT JOIN `initial_population_data` ON " +
			       "(`initial_population`.`id` = `initial_population_data`.`id`)", delegate (IDataReader reader) {
				Records.InitialSolution solution = new Records.InitialSolution();
				
				for (int i = 0; i < reader.FieldCount; ++i)
				{
					string name = reader.GetName(i);
				
					if (name.StartsWith("_p_"))
					{
						solution.Parameters[name.Substring(3)] = As<double>(reader[i]);
					}
					else if (name.StartsWith("_d_"))
					{
						solution.Data[name.Substring(3)] = As<string>(reader[i]);
					}
				}
				
				ret.Population.Add(solution);
				
				return true;
			});
			
			return ret;
		}
	}
}
