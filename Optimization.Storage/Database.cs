/*
 *  Storage.cs - This file is part of optimization-sharp
 *
 *  Copyright (C) 2011 - Jesse van den Kieboom
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
using Mono.Data.Sqlite;
using System.Reflection;
using System.Xml.Serialization;
using System.Collections.Generic;
using Biorob.Math;
using System.Text;
using System.IO;
using System.Collections;
using System.Data.Common;

namespace Optimization.Storage
{
	public class Database
	{
		public delegate bool RowCallback(IDataReader reader);

		private SqliteConnection d_connection;
		private string d_uri;

		public Database(string uri)
		{
			d_uri = uri;
		}
		
		public Database()
		{
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

		public void Close()
		{
			d_connection.Close();
		}

		public virtual void Open()
		{
			d_connection = new SqliteConnection("URI=file:" + Uri + ",version=3,busy_timeout=15000");
			d_connection.Open();
		}
		
		public static implicit operator bool(Database s)
		{
			return s.d_connection.State != ConnectionState.Closed;
		}

		public long LastInsertId
		{
			get
			{
				return (long)QueryValue("SELECT last_insert_rowid()");
			}
		}

		private string NormalizeName(string name)
		{
			return name.Replace("`", "").Replace("'", "").Replace("\"", "");
		}
		
		public DbTransaction BeginTransaction()
		{
			return d_connection.BeginTransaction();
		}

		public bool Query(ref IDbCommand cmd, params object[] parameters)
		{
			return Query(ref cmd, null, parameters);
		}

		public bool Query(ref IDbCommand cmd, string s, params object[] parameters)
		{
			return Query(s, null, ref cmd, parameters);
		}
		
		public bool Query(string s, RowCallback cb, params object[] parameters)
		{
			IDbCommand cmd = null;
			bool ret = Query(s, cb, ref cmd, parameters);

			cmd.Dispose();
			cmd = null;

			return ret;
		}

		public bool Query(string s, RowCallback cb, ref IDbCommand cmd, params object[] parameters)
		{
			var ss = new System.Diagnostics.Stopwatch();
			ss.Start();

			if (cmd == null)
			{
				SqliteCommand scmd = d_connection.CreateCommand();
				scmd.CommandText = s;

				for (int idx = 0; idx < parameters.Length; ++idx)
				{
					scmd.Parameters.AddWithValue("@" + idx, parameters[idx]);
				}

				cmd = scmd;
			}
			else
			{
				SqliteCommand scmd = (SqliteCommand)cmd;

				for (int idx = 0; idx < parameters.Length; ++idx)
				{
					scmd.Parameters[idx].Value = parameters[idx];
				}
			}

			var paramst = ss.Elapsed.TotalSeconds;

			bool ret = false;

			if (cb == null)
			{
				ret = cmd.ExecuteNonQuery() > 0;
			}
			else
			{
				IDataReader reader;

				reader = cmd.ExecuteReader();

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
		
		public void ImportTable(Database other, string table)
		{
			DataTable schema = other.d_connection.GetSchema("Columns", new string[] {null, null, table, null});
			StringBuilder q = new StringBuilder();
			
			q.AppendFormat("CREATE TABLE `{0}` (", table);
			int paridx = 0;
			List<object> pars = new List<object>();
			bool first = true;
			
			foreach (DataRow row in schema.Rows)
			{
				if (!first)
				{
					q.AppendFormat(", ");
				}

				q.AppendFormat("`{0}` {1}", row["COLUMN_NAME"], row["DATA_TYPE"]);
				
				if ((bool)row["PRIMARY_KEY"])
				{
					q.Append(" PRIMARY KEY");
					
					if ((bool)row["AUTOINCREMENT"])
					{
						q.Append(" AUTOINCREMENT");
					}
				}
				if ((bool)row["COLUMN_HASDEFAULT"])
				{
					q.AppendFormat(" DEFAULT @{0}", paridx++);
					pars.Add(row["COLUMN_DEFAULT"]);
				}
				
				first = false;
			}

			q.Append(")");
			Query(q.ToString(), pars);
			
			DbTransaction transaction = BeginTransaction();

			// Copy data
			other.Query("SELECT * FROM " + table, delegate (IDataReader reader) {
				DataTable tbl = new DataTable();
				tbl.Load(reader);
				
				List<string> columns = new List<string>();
				
				foreach (DataColumn column in tbl.Columns)
				{
					columns.Add(column.ColumnName);
				}
				
				foreach (DataRow row in tbl.Rows)
				{
					List<string> vals = new List<string>();
					object[] pp = row.ItemArray;
					
					for (int i = 0; i < pp.Length; ++i)
					{
						vals.Add(String.Format("@{1}", columns[i], i));
					}
					
					Query("INSERT INTO `" + table + "` (" + String.Join(", ", Array.ConvertAll(columns.ToArray(), a => String.Format("`{0}`", a))) + ") VALUES (" + String.Join(", ", vals.ToArray()) + ")", pp);
				}
				
				return false;
			});
			
			transaction.Commit();
		}
		
		public SqliteConnection Connection
		{
			get
			{
				return d_connection;
			}
		}
	}
}
