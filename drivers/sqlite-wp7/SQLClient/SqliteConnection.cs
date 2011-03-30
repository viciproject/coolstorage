//
// Community.CsharpSqlite.SQLiteClient.SqliteConnection.cs
//
// Represents an open connection to a Sqlite database file.
//
// Author(s): Vladimir Vukicevic  <vladimir@pobox.com>
//            Everaldo Canuto  <everaldo_canuto@yahoo.com.br>
//            Daniel Morgan <monodanmorg@yahoo.com>
//            Noah Hart <Noah.Hart@gmail.com>
//
// Copyright (C) 2002  Vladimir Vukicevic
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Data;
using System.Text;

namespace Community.CsharpSqlite.SQLiteClient
{
    [Flags]
    public enum ConnectionState
    {
        Closed = 0,
        Open = 1,
        Connecting = 2,
        Executing = 4,
        Fetching = 8,
        Broken = 16,
    }

    public class SqliteConnection : IDisposable
    {
        private string conn_str;
        private string db_file;
        private int db_mode;
        private int db_version;
        private string db_password;
        private IntPtr sqlite_handle;
        private Sqlite3.sqlite3 sqlite_handle2;
        private ConnectionState state;
        private Encoding encoding;
        private int busy_timeout;


        public SqliteConnection()
        {
            db_file = null;
            db_mode = 0644;
            db_version = 3;
            state = ConnectionState.Closed;
            sqlite_handle = IntPtr.Zero;
            encoding = null;
            busy_timeout = 0;
        }

        public SqliteConnection(string connstring) : this()
        {
            ConnectionString = connstring;
        }

        public void Dispose()
        {
            Close();
        }


        public string ConnectionString
        {
            get { return conn_str; }
            set { SetConnectionString(value); }
        }

        public int ConnectionTimeout
        {
            get { return 0; }
        }

        public string Database
        {
            get { return db_file; }
        }

        public ConnectionState State
        {
            get { return state; }
        }

        public Encoding Encoding
        {
            get { return encoding; }
        }

        public int Version
        {
            get { return db_version; }
        }

        public string ServerVersion
        {
            get { return Sqlite3.sqlite3_libversion(); }
        }

        internal Sqlite3.sqlite3 Handle2
        {
            get { return sqlite_handle2; }
        }

        internal IntPtr Handle
        {
            get { return sqlite_handle; }
        }


        public int LastInsertRowId
        {
            get
            {
                //if (Version == 3)
                return (int) Sqlite3.sqlite3_last_insert_rowid(Handle2);
                //return (int)Sqlite.sqlite3_last_insert_rowid (Handle);
                //else
                //	return Sqlite.sqlite_last_insert_rowid (Handle);
            }
        }

        public int BusyTimeout
        {
            get { return busy_timeout; }
            set { busy_timeout = value < 0 ? 0 : value; }
        }

        private void SetConnectionString(string connstring)
        {
            if (connstring == null)
            {
                Close();
                conn_str = null;
                return;
            }

            if (connstring != conn_str)
            {
                Close();
                conn_str = connstring;

                db_file = null;
                db_mode = 0644;

                string[] conn_pieces = connstring.Split(',');
                for (int i = 0; i < conn_pieces.Length; i++)
                {
                    string piece = conn_pieces[i].Trim();
                    if (piece.Length == 0)
                    {
                        // ignore empty elements
                        continue;
                    }
                    string[] arg_pieces = piece.Split('=');
                    if (arg_pieces.Length != 2)
                    {
                        throw new InvalidOperationException("Invalid connection string");
                    }
                    string token = arg_pieces[0].ToLower(System.Globalization.CultureInfo.InvariantCulture).Trim();
                    string tvalue = arg_pieces[1].Trim();
                    string tvalue_lc = arg_pieces[1].ToLower(System.Globalization.CultureInfo.InvariantCulture).Trim();
                    switch (token)
                    {
                        case "DataSource":
                        case "uri":
                            if (tvalue_lc.StartsWith("file://"))
                            {
                                db_file = tvalue.Substring(7);
                            }
                            else if (tvalue_lc.StartsWith("file:"))
                            {
                                db_file = tvalue.Substring(5);
                            }
                            else if (tvalue_lc.StartsWith("/"))
                            {
                                db_file = tvalue;
                            }
                            else
                            {
                                throw new InvalidOperationException("Invalid connection string: invalid URI");
                            }
                            break;

                        case "mode":
                            db_mode = Convert.ToInt32(tvalue);
                            break;

                        case "version":
                            db_version = Convert.ToInt32(tvalue);
                            if (db_version < 3) throw new InvalidOperationException("Minimum database version is 3");
                            break;

                        case "encoding": // only for sqlite2
                            encoding = Encoding.GetEncoding(tvalue);
                            break;

                        case "busy_timeout":
                            busy_timeout = Convert.ToInt32(tvalue);
                            break;

                        case "password":
                            if (!String.IsNullOrEmpty(db_password) &&
                                (db_password.Length != 34 || !db_password.StartsWith("0x")))
                                throw new InvalidOperationException(
                                    "Invalid password string: must be 34 hex digits starting with 0x");
                            db_password = tvalue;
                            break;
                    }
                }

                if (db_file == null)
                {
                    throw new InvalidOperationException("Invalid connection string: no URI");
                }
            }
        }

        internal void StartExec()
        {
            // use a mutex here
            state = ConnectionState.Executing;
        }

        internal void EndExec()
        {
            state = ConnectionState.Open;
        }


        public SqliteTransaction BeginTransaction()
        {
            if (state != ConnectionState.Open)
                throw new InvalidOperationException("Invalid operation: The connection is closed");

            SqliteTransaction t = new SqliteTransaction();

            t.Connection = this;
            SqliteCommand cmd = (SqliteCommand) this.CreateCommand();
            cmd.CommandText = "BEGIN";
            cmd.ExecuteNonQuery();
            return t;
        }


        public void Close()
        {
            if (state != ConnectionState.Open)
            {
                return;
            }

            state = ConnectionState.Closed;

            if (Version == 3)
                //Sqlite3.sqlite3_close()
                Sqlite3.sqlite3_close(sqlite_handle2);
            //else 
            //Sqlite.sqlite_close (sqlite_handle);
            sqlite_handle = IntPtr.Zero;
        }

        public void ChangeDatabase(string databaseName)
        {
            Close();
            db_file = databaseName;
            Open();
        }


        public SqliteCommand CreateCommand()
        {
            return new SqliteCommand(null, this);
        }

        public void Open()
        {
            if (conn_str == null)
            {
                throw new InvalidOperationException("No database specified");
            }

            if (state != ConnectionState.Closed)
            {
                return;
            }

            IntPtr errmsg = IntPtr.Zero;
            /*
			if (Version == 2){
				try {
					sqlite_handle = Sqlite.sqlite_open(db_file, db_mode, out errmsg);
					if (errmsg != IntPtr.Zero) {
						string msg = Marshal.PtrToStringAnsi (errmsg);
						Sqlite.sqliteFree (errmsg);
						throw new ApplicationException (msg);
					}
				} catch (DllNotFoundException) {
					db_version = 3;
				} catch (EntryPointNotFoundException) {
					db_version = 3;
				}
				
				if (busy_timeout != 0)
					Sqlite.sqlite_busy_timeout (sqlite_handle, busy_timeout);
			}
             */
            if (Version == 3)
            {
                sqlite_handle = (IntPtr) 1;
                int flags = Sqlite3.SQLITE_OPEN_NOMUTEX | Sqlite3.SQLITE_OPEN_READWRITE | Sqlite3.SQLITE_OPEN_CREATE;
                int err = Sqlite3.sqlite3_open_v2(db_file, ref sqlite_handle2, flags, null);
                //int err = Sqlite.sqlite3_open16(db_file, out sqlite_handle);
                if (err == (int) SqliteError.ERROR)
                    throw new ApplicationException(Sqlite3.sqlite3_errmsg(sqlite_handle2));
                //throw new ApplicationException (Marshal.PtrToStringUni( Sqlite.sqlite3_errmsg16 (sqlite_handle)));
                if (busy_timeout != 0)
                    Sqlite3.sqlite3_busy_timeout(sqlite_handle2, busy_timeout);
                //Sqlite.sqlite3_busy_timeout (sqlite_handle, busy_timeout);
                if (!String.IsNullOrEmpty(db_password))
                {
                    SqliteCommand cmd = (SqliteCommand) this.CreateCommand();
                    cmd.CommandText = "pragma hexkey='" + db_password + "'";
                    cmd.ExecuteNonQuery();
                }
            }
            state = ConnectionState.Open;
        }
    }
}
