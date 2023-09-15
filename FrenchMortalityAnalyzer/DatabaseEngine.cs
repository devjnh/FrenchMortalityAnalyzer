using MortalityAnalyzer.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MortalityAnalyzer
{
    class DatabaseEngine : IDisposable
    {
        public DatabaseEngine(string connectionString, string databaseProvider):this(connectionString, DbProviderFactories.GetFactory("System.data.SQLite" ))
        {
        }
        public DatabaseEngine(string connectionString, DbProviderFactory databaseProviderFactory)
        {
            ConnectionString = connectionString;
            DatabaseProviderFactory = databaseProviderFactory;
        }
        //public string DatabaseProvider { get; set; }
        public string ConnectionString { get; set; }
        public DbConnection DatabaseConnection { get; set; }
        public DbProviderFactory DatabaseProviderFactory { get; set; }
        private const string primaryKeyDeclaration = "INTEGER PRIMARY KEY AUTOINCREMENT";
        private const string stringDeclaration = "TEXT COLLATE NOCASE";

        public void Connect()
        {
            //DbProviderFactory dbProviderFactory = DbProviderFactories.GetFactory(DatabaseProvider);
            DatabaseConnection = DatabaseProviderFactory.CreateConnection();
            DatabaseConnection.ConnectionString = ConnectionString;
            DatabaseConnection.Open();
        }
        public void Prepare(DataTable dataTable, bool deleteOld = true)
        {
            _DataTable?.Dispose();
            _DataTable = dataTable;
            _DataAdapter = null;
            CreateTable(_DataTable, deleteOld);
        }

        private const int _BATCH_SIZE = 2000;
        private DataTable _DataTable;

        public void Insert(IEntry entry)
        {
            DataRow dataRow = _DataTable.NewRow();
            entry.ToRow(dataRow);
            _DataTable.Rows.Add(dataRow);
            if (_DataTable.Rows.Count > _BATCH_SIZE)
            {
                InsertDataTable(_DataTable);
                _DataTable.Rows.Clear();
            }
        }
        public void FinishInsertion()
        {
            InsertDataTable(_DataTable);
            _DataTable.Rows.Clear();
            _DataTable.Dispose();
            _DataTable = null;
        }
        private void InsertDataTable(DataTable dataTable)
        {
            if (dataTable.Rows.Count == 0)
                return;
            InsertRows(dataTable);
            CheckErrors(dataTable);
        }
        private void InsertRows(DataTable dataTable)
        {
            try
            {
                using (DbTransaction transaction = DatabaseConnection.BeginTransaction())
                {
                    InsertRows(dataTable, transaction);
                    transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Exception in row insertion\n{ex.Message}");
            }
        }

        DbCommand _InsertCommand;
        DbCommandBuilder _CommandBuilder;
        private DbCommand GetInsertCommand(DbDataAdapter dataAdapter, DbTransaction transaction, string tableName)
        {
            using (DbCommand selectCommand = DatabaseConnection.CreateCommand())
            {
                selectCommand.CommandText = $"SELECT * FROM {tableName}";
                selectCommand.Transaction = transaction;
                dataAdapter.SelectCommand = selectCommand;
                _CommandBuilder = DbFactory.CreateCommandBuilder();
                _CommandBuilder.DataAdapter = dataAdapter;
                _InsertCommand = _CommandBuilder.GetInsertCommand();
                dataAdapter.SelectCommand = null;
            }
            return _InsertCommand;
        }
        DbDataAdapter _DataAdapter;
        private DbDataAdapter GetDataAdapter(DbTransaction transaction, string tableName)
        {
            if (_DataAdapter == null)
            {
                _DataAdapter = DbFactory.CreateDataAdapter();
                _DataAdapter.ContinueUpdateOnError = true;
                DbCommand insertCommand = GetInsertCommand(_DataAdapter, transaction, tableName);
                _DataAdapter.InsertCommand = insertCommand;
            }

            return _DataAdapter;
        }
        private void InsertRows(DataTable dataTable, DbTransaction transaction)
        {
            DbDataAdapter dataAdapter = GetDataAdapter(transaction, dataTable.TableName);
            dataAdapter.InsertCommand.Transaction = transaction;
            dataAdapter.Update(dataTable);
        }

        private DbProviderFactory _DbProviderFactory;
        private DbProviderFactory DbFactory
        {
            get
            {
                if (_DbProviderFactory == null)
                    _DbProviderFactory = DbProviderFactories.GetFactory(DatabaseConnection);
                return _DbProviderFactory;
            }
        }

        private static void CheckErrors(DataTable dataTable)
        {
            foreach (DataRow dataRow in dataTable.Rows)
            {
                if (dataRow.HasErrors)
                {
                    Console.WriteLine($"Unable to insert a row: {dataRow.RowError}");
                }
            }
        }
        public void Dispose()
        {
            DatabaseConnection?.Dispose();
            _DataTable?.Dispose();
            _InsertCommand?.Dispose();
            _CommandBuilder?.Dispose();
            _DataAdapter?.Dispose();
            DatabaseConnection = null;
            _DataTable = null;
            _InsertCommand = null;
            _CommandBuilder = null;
            _DataAdapter = null;
        }

        public DbDataReader GetDataReader(string sqlCommand)
        {
            DbCommand command = DatabaseConnection.CreateCommand();
            command.CommandText = sqlCommand;
            return command.ExecuteReader();
        }

        public DataTable GetDataTable(string sqlCommand)
        {
            DataTable dataTable = new DataTable();
            FillDataTable(sqlCommand, dataTable);
            return dataTable;
        }

        public void FillDataTable(string sqlCommand, DataTable dataTable)
        {
            using (DbCommand command = DatabaseConnection.CreateCommand())
            {
                command.CommandText = sqlCommand;
                using (DbDataAdapter dataAdapter = DatabaseProviderFactory.CreateDataAdapter())
                {
                    dataAdapter.SelectCommand = command;
                    dataAdapter.Fill(dataTable);
                }
            }
        }

        public object GetValue(string sqlCommand)
        {
            using (DbCommand command = DatabaseConnection.CreateCommand())
            {
                command.CommandText = sqlCommand;
                return command.ExecuteScalar();
            }
        }

        public void InsertTable(DataTable dataTable)
        {
            CreateTable(dataTable);
            _DataAdapter = null;
            InsertDataTable(dataTable);
        }

        private string GetDbType(Type type)
        {
            if (type == typeof(int))
                return "int";
            else if (type == typeof(double))
                return "float";
            else if (type == typeof(DateTime))
                return "datetime";
            else
                return stringDeclaration;
        }
        private void CreateTable(DataTable dataTable, bool deleteOld = true)
        {
            StringBuilder commandBuilder = new StringBuilder($"CREATE TABLE {dataTable.TableName} (Id {primaryKeyDeclaration}");
            foreach (DataColumn dataColumn in dataTable.Columns)
                commandBuilder.Append($", {dataColumn.ColumnName} {GetDbType(dataColumn.DataType)}");
            commandBuilder.Append(')');
            using (DbCommand command = DatabaseConnection.CreateCommand())
            {
                if (deleteOld)
                    try
                    {
                        command.CommandText = $"DROP TABLE {dataTable.TableName}";
                        command.ExecuteNonQuery();
                    }
                    catch { }

                try
                {
                    command.CommandText = commandBuilder.ToString();
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    if (deleteOld)
                        throw;
                }
            }
        }
        public bool DoesTableExist(string tableName)
        {
            try
            {
                using (DbCommand command = DatabaseConnection.CreateCommand())
                {
                    command.CommandText = $"SELECT * FROM {tableName} LIMIT 1";
                    command.ExecuteNonQuery();
                }
                return true;
            }
            catch
            {
                return false;
            }

        }
    }
}
