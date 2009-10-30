/*
 *  SQLite.cs - This file is part of optimization-sharp
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

namespace Optimization.Storage
{
	public class SQLite : Storage
	{
		private delegate bool RowCallback(IDataReader reader);
		private SqliteConnection d_connection;
		
		public SQLite(Job job) : base(job)
		{
		}
		
		private Optimizer Optimizer
		{
			get
			{
				return Job.Optimizer;
			}
		}
		
		public override void Begin()
		{
			if (String.IsNullOrEmpty(Uri))
			{
				return;
			}
			
			Uri = UniqueFile(Uri);
			
			d_connection = new SqliteConnection("URI=file:" + Uri + ",version=3");
			d_connection.Open();
			
			CreateTables();
		}
		
		private string UniqueFile(string filename)
		{
			string orig = Path.GetFullPath(filename);
			string unique = orig;
			int i = 1;
			
			while (File.Exists(unique))
			{
				unique = orig + "." + i;
				++i;
			}
			
			return unique;
		}
		
		public override void End()
		{
			d_connection.Close();
		}
		
		private string NormalizeName(string name)
		{
			return name.Replace("`", "").Replace("'", "").Replace("\"", "");
		}
		
		private void InitializeFitnessTable()
		{
			if (Optimizer.Population.Count == 0)
			{
				return;
			}
			
			Solution solution = Optimizer.Population[0];
			StringBuilder builder = new StringBuilder();
			
			Query("DROP INDEX IF EXISTS fitness_index");
			Query("DROP INDEX IF EXISTS iteration");
			Query("DROP TABLE IF EXISTS `fitness`");
			
			builder.Append("CREATE TABLE `fitness` (`iteration` INT, `index` INT, `value` DOUBLE");
			
			foreach (KeyValuePair<string, double> pair in solution.Fitness.Values)	
			{
				string norm = NormalizeName(pair.Key);
				
				builder.AppendFormat(", `_{0}`", norm);
			}
			
			builder.Append(")");
			
			Query(builder.ToString());

			Query("CREATE INDEX fitness_index ON fitness(`index`)");
			Query("CREATE INDEX fitness_iteration ON fitness(`iteration`)");
		}
		
		private void InitializeSolutionData()
		{
			if (Optimizer.Population.Count == 0)
			{
				return;
			}
			
			Solution solution = Optimizer.Population[0];
			
			foreach (KeyValuePair<string, object> pair in solution.Data)
			{
				Query("ALTER TABLE `solution` ADD COLUMN `_" + NormalizeName(pair.Key) + "` TEXT");
			}
		}
		
		private void InitializeFirst()
		{
			Query("BEGIN TRANSACTION");
			InitializeFitnessTable();
			InitializeSolutionData();
			Query("COMMIT");
		}
		
		delegate object ParameterValueFunc(Parameter parameter);
		
		private string Serialize(List<Parameter> parameters, ParameterValueFunc func)
		{
			List<string> ret = new List<string>();
			
			foreach (Parameter parameter in parameters)
			{
				ret.Add(func(parameter).ToString());
			}
			
			return String.Join(",", ret.ToArray());
		}
		
		private void Save(Solution solution)
		{
			Query("INSERT INTO `solution` (`index`, `iteration`, `values`, `value_names`, `fitness`) VALUES (@0, @1, @2, @3, @4)",
			      solution.Id,
			      Optimizer.CurrentIteration,
			      Serialize(solution.Parameters, delegate (Parameter param) { return param.Value; }),
			      Serialize(solution.Parameters, delegate (Parameter param) { return param.Name; }),
			      solution.Fitness.Value);
			
			StringBuilder builder = new StringBuilder();
			builder.Append("INSERT INTO `fitness` (`index`, `iteration`, `value`");
			
			List<object> vals = new List<object>();
			vals.Add(solution.Id);
			vals.Add(Optimizer.CurrentIteration);
			vals.Add(solution.Fitness.Value);
			
			StringBuilder values = new StringBuilder();
			values.Append("VALUES(@0, @1, @2");
			
			int i = 3;
			
			foreach (KeyValuePair<string, double> pair in solution.Fitness.Values)	
			{
				string norm = NormalizeName(pair.Key);
				
				builder.AppendFormat(", `_{0}`", norm);
				values.AppendFormat(", @{0}", i);
				
				vals.Add(pair.Value.ToString());
				
				++i;
			}
			
			builder.AppendFormat(") {0})", values);		
			Query(builder.ToString(), vals.ToArray());
			
			foreach (KeyValuePair<string, object> pair in solution.Data)
			{
				Query("UPDATE `solution` SET `_" + NormalizeName(pair.Key) + "` = @0 WHERE `index` = @1 AND `iteration` = @2",
				      pair.Value.ToString(), solution.Id, Optimizer.CurrentIteration);
			}
		}
		
		public override void SaveIteration()
		{
			if (Optimizer.CurrentIteration == 0)
			{
				InitializeFirst();
			}

			Query("BEGIN TRANSACTION");
			
			int bestid = -1;
			double bestfitness = 0;

			if (Optimizer.Best != null)
			{
				bestid = (int)Optimizer.Best.Id;
				bestfitness = Optimizer.Best.Fitness.Value;
			}

			Query("INSERT INTO `iteration` (`iteration`, `best_id`, `best_fitness`, `time`) VALUES(@0, @1, @2, @3)",
			      Optimizer.CurrentIteration, bestid, bestfitness, UnixTimeStamp);
			
			foreach (Solution solution in Optimizer)
			{
				Save(solution);
			}

			Query("COMMIT");
		}
		
		private void CreateTables()
		{
			Query("BEGIN TRANSACTION");
			Query("CREATE TABLE IF NOT EXISTS `settings` (`name` TEXT, `value` TEXT)");
			Query("DELETE FROM `settings`");
			
			Settings settings = Optimizer.Configuration;
			
			// Settings
			foreach (KeyValuePair<string, object> pair in settings)
			{
				Query("INSERT INTO `settings` (`name`, `value`) VALUES(@0, @1)", pair.Key, pair.Value.ToString());
			}
			
			Query("INSERT INTO `settings` (`name`, `value`) VALUES('job', @0)", Job.Name);
			Query("INSERT INTO `settings` (`name`, `value`) VALUES('optimizer', @0)", Optimizer.Name);
			Query("INSERT INTO `settings` (`name`, `value`) VALUES('priority', @0)", Job.Priority);
			
			Query("CREATE TABLE IF NOT EXISTS `fitness_settings` (`name` TEXT, `value` TEXT)");
			
			Query("INSERT INTO `fitness_settings` (`name`, `value`) VALUES (@0, @1)", "expression", Optimizer.Fitness.Expression.Text);
			
			foreach (KeyValuePair<string, Expression> var in Optimizer.Fitness.Variables)
			{
				Query("INSERT INTO `fitness_settings` (`name`, `value`) VALUES(@0, @1)", var.Key, var.Value.Text);
			}
			
			Query("CREATE TABLE IF NOT EXISTS `iteration` (`iteration` INT PRIMARY KEY, `best_id` INT, `best_fitness` DOUBLE, `time` INT)");
			Query("CREATE INDEX IF NOT EXISTS iteration_iteration ON iteration(`iteration`)");
			
			Query("CREATE TABLE IF NOT EXISTS `solution` (`index` INT, `iteration` INT REFERENCES `iteration` (`iteration`), `values` TEXT, `value_names` TEXT, `fitness` DOUBLE)");
			Query("CREATE INDEX IF NOT EXISTS solution_index ON solution(`index`)");
			Query("CREATE INDEX IF NOT EXISTS solution_iteration ON solution(`iteration`)");
			
			Query("CREATE TABLE IF NOT EXISTS `boundaries` (`name` TEXT, `min` DOUBLE, `max` DOUBLE)");
			Query("DELETE FROM `boundaries`");
			
			foreach (Boundary boundary in Optimizer.Boundaries)
			{
				Query("INSERT INTO `boundaries` (`name`, `min`, `max`) VALUES (@0, @1, @2)", boundary.Name, boundary.Min, boundary.Max);
			}
			
			Query("CREATE TABLE IF NOT EXISTS `parameters` (`name` TEXT, `boundary` TEXT)");
			Query("DELETE FROM parameters");
			
			foreach (Parameter parameter in Optimizer.Parameters)
			{
				Query("INSERT INTO `parameters` (`name`, `boundary`) VALUES (@0, @1)", parameter.Name, parameter.Boundary.Name);
			}
			
			Query("CREATE TABLE IF NOT EXISTS `log` (`time` INT, `type` TEXT, `message` TEXT)");
			Query("DELETE FROM `log`");
			
			Query("CREATE TABLE IF NOT EXISTS `dispatcher` (`name` TEXT, `value` TEXT)");
			Query("DELETE FROM `dispatcher`");
			
			Query("INSERT INTO `dispatcher` (`name`, `value`) VALUES ('name', @0)", Job.Dispatcher.Name);
			
			foreach (KeyValuePair<string, string> disp in Job.Dispatcher.Settings)
			{
				Query("INSERT INTO `dispatcher` (`name`, `value`) VALUES (@0, @1)", disp.Key, disp.Value);
			}
			
			Query("COMMIT");
		}
		
		private bool Query(string s, RowCallback cb, params object[] parameters)
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
		
		private bool Query(string s, params object[] parameters)
		{
			return Query(s, null, parameters);
		}
		
		private long UnixTimeStamp
		{
			get
			{
				return (long)(DateTime.UtcNow - new DateTime(1970,1,1,0,0,0)).TotalSeconds;
			}
		}
		
		public override void Log(string type, string str)
		{
			Query("INSERT INTO `log` (`time`, `type`, `message`) VALUES (@0, @1)", UnixTimeStamp, type, str);
		}
	}
}
