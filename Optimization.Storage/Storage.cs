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
using Mono.Data.SqliteClient;
using System.Reflection;
using System.Xml.Serialization;
using System.Collections.Generic;
using Optimization.Math;
using System.Text;
using System.IO;
using System.Collections;

namespace Optimization.Storage
{
	public class Storage
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

		public delegate bool RowCallback(IDataReader reader);
		private SqliteConnection d_connection;

		private string d_uri;
		private Job d_job;
		private List<LogItem> d_logBatch;

		public Storage(Job job)
		{
			d_job = job;
			d_logBatch = new List<LogItem>();
		}

		public string Uri
		{
			get
			{
				return d_uri;
			}
			set
			{
				d_uri = value;
			}
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
			CreateTables();
		}

		public void Close()
		{
			d_connection.Close();
		}

		public bool Open()
		{
			bool exists = File.Exists(Uri);

			d_connection = new SqliteConnection("URI=file:" + Uri + ",version=3,busy_timeout=15000");
			d_connection.Open();

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
				d_connection.Close();
			}

			return ret;
		}

		public static implicit operator bool(Storage s)
		{
			return s.d_connection.State != ConnectionState.Closed;
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
				System.Data.Common.DbTransaction transaction = d_connection.BeginTransaction();
				SaveLog();
				transaction.Commit();
			}

			d_connection.Close();
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
			if (Job.Optimizer.Population.Count == 0)
			{
				return;
			}

			Solution solution = Job.Optimizer.Population[0];
			StringBuilder builder = new StringBuilder();

			Query("DROP INDEX IF EXISTS parameter_values_index");
			Query("DROP INDEX IF EXISTS parameter_values_iteration");
			Query("DROP TABLE IF EXISTS `parameter_values`");

			builder.Append("CREATE TABLE `parameter_values` (`iteration` INT, `index` INT");

			foreach (Parameter parameter in solution.Parameters)
			{
				string norm = NormalizeName(parameter.Name);
				builder.AppendFormat(", `_p_{0}`", norm);
			}

			builder.Append(")");

			Query(builder.ToString());

			Query("CREATE INDEX parameter_values_index ON parameter_values(`index`)");
			Query("CREATE INDEX parameter_values_iteration ON parameter_values(`iteration`)");
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
				builder.AppendFormat(", `_s_{0}`",  NormalizeName(pair.Key));
			}

			builder.Append(")");
			Query(builder.ToString());
			Query("CREATE INDEX state_iteration ON state(`iteration`)");
		}

		private void InitializeFirst()
		{
			System.Data.Common.DbTransaction transaction = d_connection.BeginTransaction();

			InitializeFitnessTable();
			InitializeParametersTable();
			InitializeDataTable();
			InitializeStateTable();

			transaction.Commit();
		}

		delegate bool GenerateSavePair(object container, out string name, out object val);

		private void SavePairs(Solution solution, ICollection collection, string table, Dictionary<string, object> additional, GenerateSavePair generator)
		{
			StringBuilder builder = new StringBuilder();
			builder.AppendFormat("INSERT INTO `{0}` (`iteration`", table);

			List<object> vals = new List<object>();

			vals.Add(Job.Optimizer.CurrentIteration);

			StringBuilder values = new StringBuilder();
			values.Append("VALUES(@0");

			int i = 1;

			if (solution != null)
			{
				builder.Append(", `index`");
				vals.Add(solution.Id);
				values.Append(", @1");
				++i;
			}

			if (additional != null)
			{
				foreach (KeyValuePair<string, object> pair in additional)
				{
					builder.AppendFormat(", `{0}`", pair.Key);
					vals.Add(pair.Value);

					values.AppendFormat(", @{0}", i);

					++i;
				}
			}

			foreach (object o in collection)
			{
				string name;
				object val;

				if (generator(o, out name, out val))
				{
					builder.AppendFormat(", `{0}`", NormalizeName(name));
					values.AppendFormat(", @{0}", i);

					vals.Add(val);

					++i;
				}
			}

			builder.Append(") ").Append(values).Append(")");
			Query(builder.ToString(), vals.ToArray());
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
				fitvalues["_fv_" + pair.Key] = pair.Value.Expression.Evaluate(Math.Constants.Context, solution.Fitness.Context);
			}

			SavePairs(solution, fitvalues, "fitness", additional, delegate (object o, out string name, out object val) {
				KeyValuePair<string, double> pair = (KeyValuePair<string, double>)o;

				name = pair.Key;
				val = pair.Value;

				return true;
			});
		}

		private void SaveParameters(Solution solution)
		{
			SavePairs(solution, solution.Parameters, "parameter_values", null, delegate (object o, out string name, out object val) {
				Parameter parameter = (Parameter)o;

				name = "_p_" + parameter.Name;
				val = parameter.Value;

				return true;
			});
		}

		private void SaveData(Solution solution)
		{
			SavePairs(solution, solution.Data, "data", null, delegate (object o, out string name, out object val) {
				KeyValuePair<string, object> data = (KeyValuePair<string, object>)o;

				name = "_d_" + data.Key;
				val = data.Value;

				return true;
			});
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

		private void Save(Solution solution)
		{
			Query("INSERT INTO `solution` (`index`, `iteration`, `fitness`) VALUES (@0, @1, @2)",
			      solution.Id, Job.Optimizer.CurrentIteration, solution.Fitness.Value);

			SaveFitness(solution);
			SaveParameters(solution);
			SaveData(solution);
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

			System.Data.Common.DbTransaction transaction = d_connection.BeginTransaction();

			int bestid = -1;
			double bestfitness = 0;

			if (Job.Optimizer.Best != null)
			{
				bestid = (int)Job.Optimizer.Best.Id;
				bestfitness = Job.Optimizer.Best.Fitness.Value;
			}

			Query("INSERT INTO `iteration` (`iteration`, `best_id`, `best_fitness`, `time`) VALUES(@0, @1, @2, @3)",
			      Job.Optimizer.CurrentIteration, bestid, bestfitness, UnixTimeStamp);

			foreach (Solution solution in Job.Optimizer)
			{
				Save(solution);
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
			System.Data.Common.DbTransaction transaction = d_connection.BeginTransaction();

			Query("CREATE TABLE IF NOT EXISTS `job` (`name` TEXT, `optimizer` TEXT, `dispatcher` TEXT, `priority` DOUBLE, `timeout` DOUBLE, `token` TEXT)");
			Query("DELETE FROM `job`");

			Query("INSERT INTO `job` (`name`, `optimizer`, `dispatcher`, `priority`, `timeout`, `token`) VALUES(@0, @1, @2, @3, @4, @5)", Job.Name, Job.Optimizer.Name, Job.Dispatcher.Name, Job.Priority, Job.Timeout, Job.Token);

			Query("CREATE TABLE IF NOT EXISTS `settings` (`id` INTEGER PRIMARY KEY AUTOINCREMENT, `name` TEXT, `value` TEXT)");
			Query("DELETE FROM `settings`");

			Settings settings = Job.Optimizer.Configuration;

			// Settings
			foreach (KeyValuePair<string, object> pair in settings)
			{
				Query("INSERT INTO `settings` (`name`, `value`) VALUES(@0, @1)", pair.Key, pair.Value.ToString());
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

			foreach (Extension ext in Job.Optimizer.Extensions)
			{
				Query("INSERT INTO `extensions` (`name`) VALUES (@0)", Extension.GetName(ext.GetType()));
			}

			transaction.Commit();
		}

		private void CreateTables()
		{
			System.Data.Common.DbTransaction transaction = d_connection.BeginTransaction();

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

		public bool Query(string s, RowCallback cb, params object[] parameters)
		{
			IDbCommand cmd = d_connection.CreateCommand();
			cmd.CommandText = s;

			for (int idx = 0; idx < parameters.Length; ++idx)
			{
				IDbDataParameter par = cmd.CreateParameter();
				par.ParameterName = "@" + idx;
				par.Value = parameters[idx];

				cmd.Parameters.Add(par);
			}

			bool ret = false;

			if (cb == null)
			{
				try
				{
					ret = cmd.ExecuteNonQuery() > 0;
				}
				catch (Exception e)
				{
					Console.Error.WriteLine("Error in query `{0}': {1}", s, e.Message);
				}
			}
			else
			{
				IDataReader reader;

				try
				{
					reader = cmd.ExecuteReader();
				}
				catch (Exception e)
				{
					reader = null;
					Console.Error.WriteLine("Error in query `{0}': {1}", s, e.Message);
				}

				while (reader != null && reader.Read())
				{
					ret = cb(reader);

					if (!ret)
					{
						break;
					}
				}

				if (reader != null)
				{
					reader.Close();
					reader = null;
				}
			}

			cmd.Dispose();
			cmd = null;

			return ret;
		}

		public object[] QueryFirst(string s, params object[] parameters)
		{
			object[] ret = null;

			Query(s, delegate (IDataReader reader) {
				ret = new object[reader.FieldCount];

				reader.GetValues(ret);
				return false;
			}, parameters);

			return ret;
		}

		public object QueryValue(string s, params object[] parameters)
		{
			object ret = null;

			Query(s, delegate (IDataReader reader) {
				ret = reader.GetValue(0);
				return false;
			}, parameters);

			return ret;
		}

		public bool Query(string s, params object[] parameters)
		{
			return Query(s, null, parameters);
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

			solution.Index = (int)reader["the_index"];
			solution.Iteration = (int)reader["the_iteration"];

			for (int i = 0; i < reader.FieldCount; ++i)
			{
				string name = reader.GetName(i);

				if (name.StartsWith("_f_"))
				{
					solution.Fitness.Add(name.Substring(3), reader.GetDouble(i));
				}
				else if (name.StartsWith("_p_"))
				{
					solution.Parameters.Add(name.Substring(3), reader.GetDouble(i));
				}
				else if (name.StartsWith("_d_"))
				{
					solution.Data.Add(name.Substring(3), reader.GetString(i));
				}
			}

			solution.FitnessValue = (double)reader["fitness"];
			return solution;
		}

		public Records.Iteration ReadIteration(int iteration)
		{
			Records.Iteration ret = new Records.Iteration();

			object[] vals = QueryFirst("SELECT `best_id`, `time` FROM iteration WHERE `iteration` = @0", iteration);

			int bestId = (int)vals[0];

			ret.Index = iteration;
			ret.Time = FromUnixTimestamp((int)vals[1]);

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

		public Records.Job ReadJob()
		{
			Records.Job job = new Records.Job();
			object[] jobspec = QueryFirst("SELECT `name`, `priority`, `timeout`, `token`, `optimizer`, `dispatcher` FROM `job`");

			job.Name = (string)jobspec[0];
			job.Priority = (double)jobspec[1];
			job.Timeout = (double)jobspec[2];
			job.Token = (string)jobspec[3];
			job.Optimizer.Name = (string)jobspec[4];
			job.Dispatcher.Name = (string)jobspec[5];

			/* Optimizer stuff */
			Query("SELECT `name`, `value` FROM `settings`", delegate (IDataReader reader) {
				job.Optimizer.Settings[(string)reader[0]] = (string)reader[1];
				return true;
			});

			Dictionary<string, Records.Boundary> boundaries = new Dictionary<string, Records.Boundary>();

			Query(@"SELECT `name`, `min_repr`, `max_repr`, `min_initial_repr`, `max_initial_repr`
			        FROM `boundaries`", delegate (IDataReader reader) {
				Records.Boundary boundary = new Records.Boundary();

				boundary.Name = (string)reader[0];
				boundary.Min = (string)reader[1];
				boundary.Max = (string)reader[2];
				boundary.MinInitial = (string)reader[3];
				boundary.MaxInitial = (string)reader[4];

				job.Optimizer.Boundaries.Add(boundary);
				boundaries[boundary.Name] = boundary;

				return true;
			});

			Query("SELECT `name`, `boundary` FROM `parameters`", delegate (IDataReader reader) {
				Records.Parameter parameter = new Records.Parameter();
				parameter.Name = (string)reader[0];
				parameter.Boundary = boundaries[(string)reader[1]];

				job.Optimizer.Parameters.Add(parameter);
				return true;
			});

			/* Dispatcher stuff */
			Query("SELECT `name`, `value` FROM `dispatcher`", delegate (IDataReader reader) {
				job.Dispatcher.Settings[(string)reader[0]] = (string)reader[1];
				return true;
			});

			/* Fitness stuff */
			Query("SELECT `name`, `value`, `mode` FROM `fitness_settings`", delegate (IDataReader reader) {
				string name = (string)reader[0];
				string val = (string)reader[1];
				string mode = (string)reader[2];

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
			Int64 iteration = (Int64)QueryValue("SELECT COUNT(iteration) FROM iteration");

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
							job.Optimizer.State.Settings.Add(name.Substring(3), reader.GetString(i));
						}
					}

					return false;
				});
			}

			/* Extensions stuff */
			Query("SELECT `name` FROM `extensions`", delegate (IDataReader reader) {
				string name = (string)reader[0];

				job.Extensions.Add(name);
				return true;
			});

			return job;
		}

		public long ReadIterations()
		{
			return (long)QueryValue("SELECT COUNT(`iteration`) FROM iteration");
		}

		public long ReadSolutions(long iteration)
		{
			return (long)QueryValue("SELECT COUNT(*) FROM solution WHERE iteration = @0", iteration);
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

				log.Time = FromUnixTimestamp((int)reader["time"]);
				log.Type = (string)reader["type"];
				log.Message = (string)reader["message"];

				ret.Add(log);
				return true;
			});

			return ret;
		}
	}
}
