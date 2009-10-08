using System;
using System.Data;
using Mono.Data.SqliteClient;
using System.Reflection;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace Optimization.Storage
{
	public class SQLite : Storage
	{
		private delegate bool RowCallback(IDataReader reader);
		private SqliteConnection d_connection;
		
		public override void Begin ()
		{
			if (String.IsNullOrEmpty(Uri))
			{
				return;
			}
			
			d_connection = new SqliteConnection("file=" + Uri);
			d_connection.Open();
			
			CreateTables();
		}
		
		public override void End ()
		{
			d_connection.Close();
		}
		
		public override void SaveIteration ()
		{
			// TODO
		}
		
		private void CreateTables()
		{
			Query("BEGIN TRANSACTION");
			Query("CREATE TABLE IF NOT EXISTS `settings` (`name` TEXT, `value` TEXT)");
			Query("DELETE FROM `settings`");
			
			Settings settings = Optimizer.Configuration;
			
			// Typed settings
			foreach (KeyValuePair<string, object> pair in settings.TypedSettings())
			{
				Query("INSERT INTO `settings` (`name`, `value`) VALUES(@0, @1)", pair.Key, pair.Value.ToString());
			}
			
			// General settings
			foreach (KeyValuePair<string,object> pair in settings)
			{
				Query("INSERT INTO `settings` (`name`, `value`) VALUES(@0, @1)", pair.Key, pair.Value.ToString());
			}
			
			Query("CREATE TABLE IF NOT EXISTS `fitness_settings` (`name` TEXT, `value` TEXT)");
			
			// TODO: Add actual fitness settings
			
			Query("CREATE TABLE IF NOT EXISTS `iteration` (`iteration` INT PRIMARY KEY, `best_id` INT, `best_fitness` DOUBLE, `time` INT)");
			Query("CREATE TABLE IF NOT EXISTS `solution` (`index` INT, `iteration` INT REFERENCES `iteration` (`iteration`), `values` TEXT, `value_names` TEXT, `fitness` DOUBLE)");
			
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
			
			bool ret;
			
			IDataReader reader = cmd.ExecuteReader();
			
			if (cb != null)
			{
				ret = false;
				
				while (reader.Read())
				{
					ret = cb(reader);
					
					if (!ret)
					{
						break;
					}
				}
			}
			else
			{
				ret = true;
			}
			
			reader.Close();
			reader = null;
			
			cmd.Dispose();
			cmd = null;
			
			return ret;
		}
		
		private bool Query(string s, params object[] parameters)
		{
			return Query(s, null, parameters);
		}
	}
}
