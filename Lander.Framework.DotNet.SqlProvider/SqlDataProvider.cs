using System;
using System.Data.SqlClient;
using Lander.Framework.DotNet.Exceptions;
using Lander.Framework.DotNet.Log;
using System.Collections;
using System.Reflection;
using System.Data;
using System.Configuration;

namespace Lander.Framework.DotNet.SqlProvider
{
    public class SqlDataProvider : IDisposable
    {
        private int _commandTimeout = -1;
        private const string APPLICATION_NAME = "SqlDataProvider";
        private SqlConnection _connection;
        private SqlTransaction _transaction;
        private string _connectionString;
        private string _statementBeforeCloseConnection;
        private string _statementAfterOpenConnection;
        private Logger _logger;
        private bool _disposed;

        public string ConnectionString
        {
            get
            {
                return this._connectionString;
            }
            set
            {
                this._connectionString = value;
            }
        }

        public SqlTransaction Transaction
        {
            get
            {
                return this._transaction;
            }
        }

        public string StatementBeforeCloseConnection
        {
            get
            {
                return this._statementBeforeCloseConnection;
            }
            set
            {
                this._statementBeforeCloseConnection = value;
            }
        }

        public string StatementAfterOpenConnection
        {
            get
            {
                return this._statementAfterOpenConnection;
            }
            set
            {
                this._statementAfterOpenConnection = value;
            }
        }

        public SqlConnection GetConnection
        {
            get
            {
                if (this._connection == null || this._connection.State != ConnectionState.Open)
                    this._connection = this.OpenConnection();
                return this._connection;
            }
        }

        public int CommandTimeout
        {
            get
            {
                return this._commandTimeout;
            }
            set
            {
                this._commandTimeout = value;
            }
        }

        public SqlDataProvider(string pConnectionString, string pLogConfigPath)
        {
            this._connectionString = pConnectionString;
            this._logger = new Logger(MethodBase.GetCurrentMethod().DeclaringType, pLogConfigPath);
        }

        public SqlDataProvider(string pConnectionString)
        {
            this._connectionString = pConnectionString;
            this._logger = new Logger(MethodBase.GetCurrentMethod().DeclaringType, ConfigurationManager.AppSettings["DataProviderLogConfigPath"]);
        }

        ~SqlDataProvider()
        {
            this.Dispose(false);
        }

        public SqlConnection OpenConnection()
        {
            try
            {
                return this.OpenConnection(true);
            }
            catch (DataProviderApplicationException ex)
            {
                this._logger.Debug((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), (Exception)ex);
                throw;
            }
            catch (Exception ex)
            {
                this._logger.Error((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), ex);
                throw new DataProviderApplicationException("SqlDataProvider. Não foi possível abrir a conexão com o banco de dados.", ex);
            }
        }

        private SqlConnection OpenConnection(bool LeaveConnectionOpened)
        {
            SqlConnection sqlConnection1 = (SqlConnection)null;
            SqlCommand sqlCommand = (SqlCommand)null;
            SqlConnection sqlConnection2;
            try
            {
                this._logger.Debug((object)("ConnectionString: " + this._connectionString));
                sqlConnection1 = new SqlConnection();
                sqlConnection1.ConnectionString = this._connectionString;
                sqlConnection1.Open();
                this._logger.Info((object)"Conexão aberta com o banco de dados.");
                if (this._statementAfterOpenConnection != null)
                {
                    if (this._statementAfterOpenConnection.Trim().Length > 0)
                    {
                        try
                        {
                            sqlCommand = this.CreateCommand(this._statementAfterOpenConnection, new object[0]);
                            sqlCommand.Connection = sqlConnection1;
                            sqlCommand.ExecuteNonQuery();
                        }
                        finally
                        {
                            if (sqlCommand != null)
                                sqlCommand.Dispose();
                        }
                    }
                }
                this._logger.Info((object)"Comando após a abertura da conexão executado.");
                return sqlConnection1;
            }
            catch (DataProviderApplicationException ex)
            {
                if (sqlConnection1 != null)
                {
                    sqlConnection1.Dispose();
                    sqlConnection2 = (SqlConnection)null;
                }
                this._logger.Debug((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), (Exception)ex);
                throw;
            }
            catch (Exception ex)
            {
                if (sqlConnection1 != null)
                {
                    sqlConnection1.Dispose();
                    sqlConnection2 = (SqlConnection)null;
                }
                this._logger.Error((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), ex);
                throw new DataProviderApplicationException("SqlDataProvider. Não possível abrir a conexão com o banco de dados", ex);
            }
        }

        public void CloseConnection()
        {
            try
            {
                if (this._connection == null)
                    return;
                if (this._connection.State == ConnectionState.Open)
                    this._connection.Close();
                this._connection.Dispose();
                this._connection = (SqlConnection)null;
            }
            catch (DataProviderApplicationException ex)
            {
                this._logger.Debug((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), (Exception)ex);
                throw;
            }
            catch (Exception ex)
            {
                this._logger.Error((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), ex);
                throw new DataProviderApplicationException("SqlDataProvider. Erro ao tentar fechar a conexão com o banco de dados.", ex);
            }
        }

        public bool BeginTran()
        {
            try
            {
                if (this._transaction != null || this._connection == null)
                    return false;
                this._transaction = this._connection.BeginTransaction(IsolationLevel.ReadUncommitted);
                this._logger.Info((object)"Transação iniciada.");
                return true;
            }
            catch (DataProviderApplicationException ex)
            {
                this._logger.Debug((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), (Exception)ex);
                throw;
            }
            catch (Exception ex)
            {
                if (this._connection != null)
                {
                    this._connection.Dispose();
                    this._connection = (SqlConnection)null;
                }
                if (this._transaction != null)
                    this._transaction = (SqlTransaction)null;
                this._logger.Error((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), ex);
                throw new DataProviderApplicationException("SqlDataProvider. Erro durante a tentativa de iniciar a transação.", ex);
            }
        }

        public bool CommitTran()
        {
            try
            {
                if (this._transaction == null)
                    return false;
                this._transaction.Commit();
                this._logger.Info((object)"Transação finalizada.");
                return true;
            }
            catch (DataProviderApplicationException ex)
            {
                this._logger.Debug((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), (Exception)ex);
                throw;
            }
            catch (Exception ex)
            {
                if (this._connection != null)
                {
                    this._connection.Dispose();
                    this._connection = (SqlConnection)null;
                }
                this._logger.Error((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), ex);
                throw new DataProviderApplicationException("SqlDataProvider. Erro durante a tentativa de finalizar a transação.", ex);
            }
            finally
            {
                if (this._transaction != null)
                    this._transaction = (SqlTransaction)null;
            }
        }

        public bool RollBackTran()
        {
            try
            {
                if (this._transaction == null)
                    return false;
                this._transaction.Rollback();
                this._logger.Info((object)"Transação cancelada.");
                return true;
            }
            catch (DataProviderApplicationException ex)
            {
                this._logger.Debug((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), (Exception)ex);
                throw;
            }
            catch (Exception ex)
            {
                if (this._connection != null)
                {
                    this._connection.Dispose();
                    this._connection = (SqlConnection)null;
                }
                this._logger.Error((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), ex);
                throw new DataProviderApplicationException("SqlDataProvider. Erro durante a tentativa de cancelar a transação.", ex);
            }
            finally
            {
                if (this._transaction != null)
                    this._transaction = (SqlTransaction)null;
            }
        }

        public int ExecuteCommand(string pSql)
        {
            try
            {
                return this.ExecuteCommand(pSql, new object[0]);
            }
            catch (DataProviderApplicationException ex)
            {
                this._logger.Debug((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), (Exception)ex);
                throw;
            }
            catch (Exception ex)
            {
                this._logger.Error((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), ex);
                throw new DataProviderApplicationException("SqlDataProvider. Erro durante execução do comando no banco de dados.", ex);
            }
        }

        public int ExecuteCommand(string pSql, Hashtable pParameters)
        {
            SqlCommand pCommandToExecute = (SqlCommand)null;
            try
            {
                pCommandToExecute = this.CreateCommand(pSql, pParameters);
                return this.ExecuteCommand(pCommandToExecute);
            }
            catch (DataProviderApplicationException ex)
            {
                this._logger.Debug((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), (Exception)ex);
                throw;
            }
            catch (Exception ex)
            {
                this._logger.Error((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), ex);
                throw new DataProviderApplicationException("SqlDataProvider. Erro durante execução do comando no banco de dados.", ex);
            }
            finally
            {
                if (pCommandToExecute != null)
                    pCommandToExecute.Dispose();
            }
        }

        private int ExecuteCommand(string pSql, object[] pParameters)
        {
            SqlCommand pCommandToExecute = (SqlCommand)null;
            try
            {
                pCommandToExecute = this.CreateCommand(pSql, pParameters);
                return this.ExecuteCommand(pCommandToExecute);
            }
            catch (DataProviderApplicationException ex)
            {
                this._logger.Debug((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), (Exception)ex);
                throw;
            }
            catch (Exception ex)
            {
                this._logger.Error((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), ex);
                throw new DataProviderApplicationException("SqlDataProvider. Erro durante execução do comando no banco de dados.", ex);
            }
            finally
            {
                if (pCommandToExecute != null)
                    pCommandToExecute.Dispose();
            }
        }

        public int ExecuteCommand(SqlCommand pCommandToExecute)
        {
            try
            {
                pCommandToExecute.Connection = this.GetConnection;
                pCommandToExecute.Transaction = this.Transaction;
                if (this._commandTimeout > -1)
                    pCommandToExecute.CommandTimeout = this._commandTimeout;
                int num = pCommandToExecute.ExecuteNonQuery();
                this._logger.Info((object)("Comando executado. Retorno: " + num.ToString()));
                return num;
            }
            catch (DataProviderApplicationException ex)
            {
                this._logger.Debug((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), (Exception)ex);
                throw;
            }
            catch (Exception ex)
            {
                this._logger.Error((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), ex);
                throw new DataProviderApplicationException("SqlDataProvider. Erro durante execução do comando no banco de dados.", ex);
            }
            finally
            {
                this.CloseConnection();
            }
        }

        public object ExecuteScalar(string pSql)
        {
            try
            {
                return this.ExecuteScalar(pSql, new object[0]);
            }
            catch (DataProviderApplicationException ex)
            {
                this._logger.Debug((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), (Exception)ex);
                throw;
            }
            catch (Exception ex)
            {
                this._logger.Error((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), ex);
                throw new DataProviderApplicationException("SqlDataProvider. Erro durante execução do comando no banco de dados.", ex);
            }
        }

        private object ExecuteScalar(string pQueryToExecute, object[] pParameters)
        {
            SqlCommand pCommandToExecute = (SqlCommand)null;
            try
            {
                pCommandToExecute = this.CreateCommand(pQueryToExecute, pParameters);
                return this.ExecuteScalar(pCommandToExecute);
            }
            catch (DataProviderApplicationException ex)
            {
                this._logger.Debug((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), (Exception)ex);
                throw;
            }
            catch (Exception ex)
            {
                this._logger.Error((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), ex);
                throw new DataProviderApplicationException("SqlDataProvider. Erro durante execução do comando no banco de dados.", ex);
            }
            finally
            {
                if (pCommandToExecute != null)
                    pCommandToExecute.Dispose();
            }
        }

        public object ExecuteScalar(string pQueryToExecute, Hashtable pParameters)
        {
            SqlCommand pCommandToExecute = (SqlCommand)null;
            try
            {
                pCommandToExecute = this.CreateCommand(pQueryToExecute, pParameters);
                return this.ExecuteScalar(pCommandToExecute);
            }
            catch (DataProviderApplicationException ex)
            {
                this._logger.Debug((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), (Exception)ex);
                throw;
            }
            catch (Exception ex)
            {
                this._logger.Error((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), ex);
                throw new DataProviderApplicationException("SqlDataProvider. Erro durante execução do comando no banco de dados.", ex);
            }
            finally
            {
                if (pCommandToExecute != null)
                    pCommandToExecute.Dispose();
            }
        }

        public object ExecuteScalar(SqlCommand pCommandToExecute)
        {
            try
            {
                pCommandToExecute.Connection = this.GetConnection;
                pCommandToExecute.Transaction = this.Transaction;
                if (this._commandTimeout > -1)
                    pCommandToExecute.CommandTimeout = this._commandTimeout;
                object obj = pCommandToExecute.ExecuteScalar();
                this._logger.Info((object)("Comando executado. Retorno: " + obj.ToString()));
                return obj;
            }
            catch (DataProviderApplicationException ex)
            {
                this._logger.Debug((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), (Exception)ex);
                throw;
            }
            catch (Exception ex)
            {
                this._logger.Error((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), ex);
                throw new DataProviderApplicationException("SqlDataProvider. Erro durante execução do comando no banco de dados.", ex);
            }
            finally
            {
                this.CloseConnection();
            }
        }

        public DataSet RecordSet(SqlCommand pCommandToExecute, string pTableToCreate, DataSet pDataSetToFill)
        {
            DataSet dataSet = (DataSet)null;
            SqlDataAdapter sqlDataAdapter = (SqlDataAdapter)null;
            SqlConnection sqlConnection = (SqlConnection)null;
            try
            {
                this._logger.Info((object)"Chamou o método RecordSet(SqlCommand pCommandToExecute, String pTableToCreate, DataSet pDataSetToFill)");
                if (pCommandToExecute == null)
                    return (DataSet)null;
                dataSet = pDataSetToFill != null ? pDataSetToFill : new DataSet();
                if (this._commandTimeout >= 0)
                    pCommandToExecute.CommandTimeout = this._commandTimeout;
                try
                {
                    pCommandToExecute.Transaction = this.Transaction;
                    SqlConnection getConnection = this.GetConnection;
                    lock (getConnection)
                    {
                        this._logger.Debug((object)"Atribui a connection");
                        this._logger.Debug((object)("oSqlConnection.State:" + getConnection.State.ToString()));
                        pCommandToExecute.Connection = getConnection;
                        this._logger.Debug((object)" Cria o data adapter e preenche o dataset");
                        sqlDataAdapter = new SqlDataAdapter(pCommandToExecute);
                        this._logger.Debug((object)"Vai Executar o Fill do SQLDataAdapter");
                        sqlDataAdapter.Fill(dataSet, pTableToCreate);
                    }
                    this._logger.Info((object)"Query executada. Dataset preenchido.");
                    sqlConnection = (SqlConnection)null;
                }
                finally
                {
                    if (sqlDataAdapter != null)
                        sqlDataAdapter.Dispose();
                }
                return dataSet;
            }
            catch (DataProviderApplicationException ex)
            {
                this._logger.Debug((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), (Exception)ex);
                throw;
            }
            catch (Exception ex)
            {
                this._logger.Error((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), ex);
                throw new DataProviderApplicationException("SqlDataProvider. Erro durante execução do consulta ao banco de dados.", ex);
            }
            finally
            {
                if (dataSet != null)
                    dataSet.Dispose();
                this.CloseConnection();
                this._logger.Info((object)"Terminou o método RecordSet(SqlCommand pCommandToExecute, String pTableToCreate, DataSet pDataSetToFill)");
            }
        }

        public DataSet RecordSet(SqlCommand pCommandToExecute, string pTableToCreate)
        {
            try
            {
                return this.RecordSet(pCommandToExecute, pTableToCreate, new DataSet());
            }
            catch (DataProviderApplicationException ex)
            {
                this._logger.Debug((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), (Exception)ex);
                throw;
            }
            catch (Exception ex)
            {
                this._logger.Error((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), ex);
                throw new DataProviderApplicationException("SqlDataProvider. Erro durante execução do consulta ao banco de dados.", ex);
            }
        }

        public DataSet RecordSet(SqlCommand pCommandToExecute)
        {
            try
            {
                return this.RecordSet(pCommandToExecute, "RETORNO");
            }
            catch (DataProviderApplicationException ex)
            {
                this._logger.Debug((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), (Exception)ex);
                throw;
            }
            catch (Exception ex)
            {
                this._logger.Error((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), ex);
                throw new DataProviderApplicationException("SqlDataProvider. Erro durante execução do consulta ao banco de dados.", ex);
            }
        }

        public DataSet RecordSet(string pQueryToExecute, string pTableToCreate, DataSet pDataSetToFill)
        {
            SqlCommand pCommandToExecute = (SqlCommand)null;
            try
            {
                pCommandToExecute = this.CreateCommand(pQueryToExecute, new Hashtable());
                return this.RecordSet(pCommandToExecute, pTableToCreate, pDataSetToFill);
            }
            catch (DataProviderApplicationException ex)
            {
                this._logger.Debug((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), (Exception)ex);
                throw;
            }
            catch (Exception ex)
            {
                this._logger.Error((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), ex);
                throw new DataProviderApplicationException("SqlDataProvider. Erro durante execução do consulta ao banco de dados.", ex);
            }
            finally
            {
                if (pCommandToExecute != null)
                    pCommandToExecute.Dispose();
            }
        }

        public DataSet RecordSet(string pQueryToExecute, string pTableToCreate)
        {
            SqlCommand pCommandToExecute = (SqlCommand)null;
            try
            {
                pCommandToExecute = this.CreateCommand(pQueryToExecute, new Hashtable());
                return this.RecordSet(pCommandToExecute, pTableToCreate);
            }
            catch (DataProviderApplicationException ex)
            {
                this._logger.Debug((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), (Exception)ex);
                throw;
            }
            catch (Exception ex)
            {
                this._logger.Error((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), ex);
                throw new DataProviderApplicationException("SqlDataProvider. Erro durante execução do consulta ao banco de dados.", ex);
            }
            finally
            {
                if (pCommandToExecute != null)
                    pCommandToExecute.Dispose();
            }
        }

        public DataSet RecordSet(string pQueryToExecute)
        {
            try
            {
                return this.RecordSet(pQueryToExecute, "RETORNO");
            }
            catch (DataProviderApplicationException ex)
            {
                this._logger.Debug((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), (Exception)ex);
                throw;
            }
            catch (Exception ex)
            {
                this._logger.Error((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), ex);
                throw new DataProviderApplicationException("SqlDataProvider. Erro durante execução do consulta ao banco de dados.", ex);
            }
        }

        public DataSet RecordSet(string[] pQueryToExecute)
        {
            DataSet dataSet = (DataSet)null;
            try
            {
                dataSet = new DataSet();
                for (int index = 0; index < pQueryToExecute.Length; ++index)
                    dataSet.Merge(this.RecordSet(pQueryToExecute[index], "RETORNO" + (object)index));
                return dataSet;
            }
            catch (DataProviderApplicationException ex)
            {
                this._logger.Debug((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), (Exception)ex);
                throw;
            }
            catch (Exception ex)
            {
                this._logger.Error((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), ex);
                throw new DataProviderApplicationException("SqlDataProvider. Erro durante execução do consulta ao banco de dados.", ex);
            }
            finally
            {
                if (dataSet != null)
                    dataSet.Dispose();
            }
        }

        public DataSet RecordSet(SqlCommand[] pQueryToExecute)
        {
            DataSet dataSet = (DataSet)null;
            try
            {
                dataSet = new DataSet();
                for (int index = 0; index < pQueryToExecute.Length; ++index)
                    dataSet.Merge(this.RecordSet(pQueryToExecute[index], "RETORNO" + (object)index));
                return dataSet;
            }
            catch (DataProviderApplicationException ex)
            {
                this._logger.Debug((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), (Exception)ex);
                throw;
            }
            catch (Exception ex)
            {
                this._logger.Error((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), ex);
                throw new DataProviderApplicationException("SqlDataProvider. Erro durante execução do consulta ao banco de dados.", ex);
            }
            finally
            {
                if (dataSet != null)
                    dataSet.Dispose();
            }
        }

        public DataSet RecordSet(Hashtable pQueryToExecute)
        {
            DataSet dataSet = (DataSet)null;
            IDictionaryEnumerator dictionaryEnumerator = (IDictionaryEnumerator)null;
            try
            {
                dataSet = new DataSet();
                dictionaryEnumerator = pQueryToExecute.GetEnumerator();
                while (dictionaryEnumerator.MoveNext())
                    dataSet.Merge(this.RecordSet(dictionaryEnumerator.Value.ToString(), dictionaryEnumerator.Key.ToString()));
                return dataSet;
            }
            catch (DataProviderApplicationException ex)
            {
                this._logger.Debug((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), (Exception)ex);
                throw;
            }
            catch (Exception ex)
            {
                this._logger.Error((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), ex);
                throw new DataProviderApplicationException("SqlDataProvider. Erro durante execução do consulta ao banco de dados.", ex);
            }
            finally
            {
                if (dataSet != null)
                    dataSet.Dispose();
                if (dictionaryEnumerator != null)
                    ;
            }
        }

        public DataSet RecordSet(string pQueryToExecute, Hashtable pParameters)
        {
            try
            {
                return this.RecordSet(pQueryToExecute, pParameters, "RETORNO");
            }
            catch (DataProviderApplicationException ex)
            {
                this._logger.Debug((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), (Exception)ex);
                throw;
            }
            catch (Exception ex)
            {
                this._logger.Error((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), ex);
                throw new DataProviderApplicationException("SqlDataProvider. Erro durante execução do consulta ao banco de dados.", ex);
            }
        }

        public DataSet RecordSet(string pQueryToExecute, Hashtable pParameters, string pTableToCreate)
        {
            SqlCommand pCommandToExecute = (SqlCommand)null;
            try
            {
                pCommandToExecute = this.CreateCommand(pQueryToExecute, pParameters);
                return this.RecordSet(pCommandToExecute, pTableToCreate);
            }
            catch (DataProviderApplicationException ex)
            {
                this._logger.Debug((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), (Exception)ex);
                throw;
            }
            catch (Exception ex)
            {
                this._logger.Error((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), ex);
                throw new DataProviderApplicationException("SqlDataProvider. Erro durante execução do consulta ao banco de dados.", ex);
            }
            finally
            {
                if (pCommandToExecute != null)
                    pCommandToExecute.Dispose();
            }
        }

        private DataSet RecordSet(string pQueryToExecute, object[] pParameters)
        {
            try
            {
                return this.RecordSet(pQueryToExecute, pParameters, "RETORNO");
            }
            catch (DataProviderApplicationException ex)
            {
                this._logger.Debug((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), (Exception)ex);
                throw;
            }
            catch (Exception ex)
            {
                this._logger.Error((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), ex);
                throw new DataProviderApplicationException("SqlDataProvider. Erro durante execução do consulta ao banco de dados.", ex);
            }
        }

        private DataSet RecordSet(string pQueryToExecute, object[] pParameters, string pTableToCreate, DataSet pDataSetToFill)
        {
            SqlCommand pCommandToExecute = (SqlCommand)null;
            try
            {
                pCommandToExecute = this.CreateCommand(pQueryToExecute, pParameters);
                return this.RecordSet(pCommandToExecute, pTableToCreate, pDataSetToFill);
            }
            catch (DataProviderApplicationException ex)
            {
                this._logger.Debug((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), (Exception)ex);
                throw;
            }
            catch (Exception ex)
            {
                this._logger.Error((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), ex);
                throw new DataProviderApplicationException("SqlDataProvider. Erro durante execução do consulta ao banco de dados.", ex);
            }
            finally
            {
                if (pCommandToExecute != null)
                    pCommandToExecute.Dispose();
            }
        }

        private DataSet RecordSet(string pQueryToExecute, object[] pParameters, string pTableToCreate)
        {
            SqlCommand pCommandToExecute = (SqlCommand)null;
            try
            {
                pCommandToExecute = this.CreateCommand(pQueryToExecute, pParameters);
                return this.RecordSet(pCommandToExecute, pTableToCreate);
            }
            catch (DataProviderApplicationException ex)
            {
                this._logger.Debug((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), (Exception)ex);
                throw;
            }
            catch (Exception ex)
            {
                this._logger.Error((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), ex);
                throw new DataProviderApplicationException("SqlDataProvider. Erro durante execução do consulta ao banco de dados.", ex);
            }
            finally
            {
                if (pCommandToExecute != null)
                    pCommandToExecute.Dispose();
            }
        }

        public DataSet RecordSetDataReader(string pCommandToExecute, ref DataSet pDataSetToFill)
        {
            SqlDataReader sqlDataReader = (SqlDataReader)null;
            try
            {
                SqlCommand sqlCommand = new SqlCommand(pCommandToExecute, this.GetConnection);
                sqlCommand.Transaction = this.Transaction;
                if (this._commandTimeout >= 0)
                    sqlCommand.CommandTimeout = this._commandTimeout;
                sqlDataReader = sqlCommand.ExecuteReader();
                if (pDataSetToFill == null)
                    pDataSetToFill = new DataSet();
                if (pDataSetToFill.Tables.Count == 0)
                    pDataSetToFill.Tables.Add();
                while (sqlDataReader.Read())
                {
                    object[] objArray = new object[sqlDataReader.FieldCount];
                    for (int ordinal = 0; ordinal < sqlDataReader.FieldCount; ++ordinal)
                        objArray[ordinal] = sqlDataReader.GetValue(ordinal);
                    pDataSetToFill.Tables[0].Rows.Add(objArray);
                }
                return (DataSet)null;
            }
            catch (DataProviderApplicationException ex)
            {
                this._logger.Debug((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), (Exception)ex);
                throw;
            }
            catch (Exception ex)
            {
                this._logger.Error((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), ex);
                throw new DataProviderApplicationException("SqlDataProvider. Erro durante execução do consulta ao banco de dados.", ex);
            }
            finally
            {
                if (sqlDataReader != null)
                    ;
                this.CloseConnection();
            }
        }

        public string[] QueryInsert(DataRow[] pDataRowTableToInsert, string pTableToInsert)
        {
            string str1 = "";
            try
            {
                foreach (DataColumn column in (InternalDataCollectionBase)pDataRowTableToInsert[0].Table.Columns)
                    str1 = str1 + (object)',' + column.ColumnName;
                string str2 = str1.Substring(1);
                return this.QueryInsert(pDataRowTableToInsert, str2.Split(",".ToCharArray()), pTableToInsert);
            }
            catch (DataProviderApplicationException ex)
            {
                this._logger.Debug((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), (Exception)ex);
                throw;
            }
            catch (Exception ex)
            {
                this._logger.Error((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), ex);
                throw new DataProviderApplicationException("SqlDataProvider. Erro durante a tentativa de inserção no banco de dados.", ex);
            }
        }

        public string[] QueryInsert(DataTable pDataTableToInsert, string pTableToInsert)
        {
            string str1 = string.Empty;
            try
            {
                foreach (DataColumn column in (InternalDataCollectionBase)pDataTableToInsert.Columns)
                    str1 = str1 + (object)',' + column.ColumnName;
                string str2 = str1.Substring(1);
                return this.QueryInsert(pDataTableToInsert, str2.Split(",".ToCharArray()), pTableToInsert);
            }
            catch (DataProviderApplicationException ex)
            {
                this._logger.Debug((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), (Exception)ex);
                throw;
            }
            catch (Exception ex)
            {
                this._logger.Error((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), ex);
                throw new DataProviderApplicationException("SqlDataProvider. Erro durante a tentativa de inserção no banco de dados.", ex);
            }
        }

        public string[] QueryInsert(DataTable pDataTableToInsert, string[] pCamposInsert, string pTableToInsert)
        {
            try
            {
                return this.QueryInsert(pDataTableToInsert.Select(), pCamposInsert, pTableToInsert);
            }
            catch (DataProviderApplicationException ex)
            {
                this._logger.Debug((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), (Exception)ex);
                throw;
            }
            catch (Exception ex)
            {
                this._logger.Error((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), ex);
                throw new DataProviderApplicationException("SqlDataProvider. Erro durante a tentativa de inserção no banco de dados.", ex);
            }
        }

        public string[] QueryInsert(DataRow[] pDataTableToInsert, string[] pCamposInsert, string pTableToInsert)
        {
            string str1 = string.Empty;
            try
            {
                string[] strArray = new string[pDataTableToInsert.Length];
                int index1 = 0;
                foreach (DataRow dataRow in pDataTableToInsert)
                {
                    string str2 = "";
                    foreach (string index2 in pCamposInsert)
                    {
                        str1 = str1 + (object)',' + this.FormatColumn(dataRow[index2]);
                        str2 = str2 + (object)',' + index2;
                    }
                    strArray.SetValue((object)("INSERT INTO " + pTableToInsert + " (" + str2.Substring(1) + ") values (" + str1.Substring(1) + ")"), index1);
                    ++index1;
                    str1 = "";
                }
                return strArray;
            }
            catch (DataProviderApplicationException ex)
            {
                this._logger.Debug((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), (Exception)ex);
                throw;
            }
            catch (Exception ex)
            {
                this._logger.Error((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), ex);
                throw new DataProviderApplicationException("SqlDataProvider. Erro durante a tentativa de inserção no banco de dados.", ex);
            }
        }

        public string[] QueryUpdate(DataTable pDataTableToInsert, string pTableToInsert, string[] pCamposChave)
        {
            string str1 = string.Empty;
            try
            {
                foreach (DataColumn column in (InternalDataCollectionBase)pDataTableToInsert.Columns)
                    str1 = str1 + (object)',' + column.ColumnName;
                string str2 = str1.Substring(1);
                return this.QueryUpdate(pDataTableToInsert, pTableToInsert, pCamposChave, str2.Split(",".ToCharArray()));
            }
            catch (DataProviderApplicationException ex)
            {
                this._logger.Debug((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), (Exception)ex);
                throw;
            }
            catch (Exception ex)
            {
                this._logger.Error((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), ex);
                throw new DataProviderApplicationException("SqlDataProvider. Erro durante a tentativa de atualização no banco de dados.", ex);
            }
        }

        public string[] QueryUpdate(DataTable pDataTableToInsert, string pTableToInsert, string[] pCamposChave, string[] pCamposInsert)
        {
            try
            {
                string[] strArray = new string[pDataTableToInsert.Rows.Count];
                int index1 = 0;
                foreach (DataRow row in (InternalDataCollectionBase)pDataTableToInsert.Rows)
                {
                    string str = "";
                    foreach (string index2 in pCamposInsert)
                        str = str + "," + index2 + " = " + this.FormatColumn(row[index2]);
                    strArray.SetValue((object)("UPDATE  " + pTableToInsert + " SET " + str.Substring(1) + " WHERE " + this.GeraCondicaoWhere(row, pCamposChave)), index1);
                    ++index1;
                }
                return strArray;
            }
            catch (DataProviderApplicationException ex)
            {
                this._logger.Debug((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), (Exception)ex);
                throw;
            }
            catch (Exception ex)
            {
                this._logger.Error((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), ex);
                throw new DataProviderApplicationException("SqlDataProvider. Erro durante a tentativa de atualização no banco de dados.", ex);
            }
        }

        public string[] QueryUpdate(DataRow[] pDataTableToInsert, string pTableToInsert, string[] pCamposChave, string[] pCamposInsert)
        {
            try
            {
                string[] strArray = new string[pDataTableToInsert.Length];
                int index1 = 0;
                foreach (DataRow pRow in pDataTableToInsert)
                {
                    string str = "";
                    foreach (string index2 in pCamposInsert)
                        str = str + "," + index2 + " = " + this.FormatColumn(pRow[index2]);
                    strArray.SetValue((object)("UPDATE  " + pTableToInsert + " SET " + str.Substring(1) + " WHERE " + this.GeraCondicaoWhere(pRow, pCamposChave)), index1);
                    ++index1;
                }
                return strArray;
            }
            catch (DataProviderApplicationException ex)
            {
                this._logger.Debug((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), (Exception)ex);
                throw;
            }
            catch (Exception ex)
            {
                this._logger.Error((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), ex);
                throw new DataProviderApplicationException("SqlDataProvider. Erro durante a tentativa de atualização no banco de dados.", ex);
            }
        }

        private SqlCommand CreateCommand(string pQueryToExecute, object[] pParameters)
        {
            SqlCommand sqlCommand = (SqlCommand)null;
            try
            {
                int num = 0;
                sqlCommand = new SqlCommand(pQueryToExecute);
                foreach (object pParameter in pParameters)
                {
                    sqlCommand.Parameters.AddWithValue(num.ToString(), this.GetNullValue(pParameter));
                    ++num;
                }
                return sqlCommand;
            }
            catch (DataProviderApplicationException ex)
            {
                if (sqlCommand != null)
                {
                    sqlCommand.Dispose();
                    sqlCommand = (SqlCommand)null;
                }
                this._logger.Debug((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), (Exception)ex);
                throw;
            }
            catch (Exception ex)
            {
                if (sqlCommand != null)
                {
                    sqlCommand.Dispose();
                    sqlCommand = (SqlCommand)null;
                }
                this._logger.Error((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), ex);
                throw new DataProviderApplicationException("SqlDataProvider. Erro durante criação do comando.", ex);
            }
            finally
            {
                if (sqlCommand != null)
                    ;
            }
        }

        private SqlCommand CreateCommand(string pQueryToExecute, Hashtable pParameters)
        {
            SqlCommand sqlCommand = (SqlCommand)null;
            SqlParameter sqlParameter = (SqlParameter)null;
            try
            {
                IDictionaryEnumerator enumerator = pParameters.GetEnumerator();
                sqlCommand = new SqlCommand(pQueryToExecute);
                while (enumerator.MoveNext())
                {
                    sqlParameter = new SqlParameter(enumerator.Key.ToString(), this.GetNullValue(enumerator.Value));
                    sqlCommand.Parameters.Add(sqlParameter);
                }
                return sqlCommand;
            }
            catch (DataProviderApplicationException ex)
            {
                if (sqlCommand != null)
                {
                    sqlCommand.Dispose();
                    sqlCommand = (SqlCommand)null;
                }
                this._logger.Debug((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), (Exception)ex);
                throw;
            }
            catch (Exception ex)
            {
                if (sqlCommand != null)
                {
                    sqlCommand.Dispose();
                    sqlCommand = (SqlCommand)null;
                }
                this._logger.Error((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), ex);
                throw new DataProviderApplicationException("SqlDataProvider. Erro durante criação do comando.", ex);
            }
            finally
            {
                if (sqlCommand != null)
                    ;
                if (sqlParameter != null)
                    ;
            }
        }

        public string GeraCondicaoWhere(DataRow pRow, string[] pCamposChave)
        {
            string str1 = string.Empty;
            try
            {
                foreach (string index in pCamposChave)
                {
                    string str2 = this.FormatColumn(pRow[index]);
                    string str3 = !str2.ToUpper().ToString().Equals("NULL") ? " = " + str2 : "IS NULL";
                    str1 = str1 + " AND " + index + str3;
                }
                return str1.Substring(4);
            }
            catch (DataProviderApplicationException ex)
            {
                this._logger.Debug((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), (Exception)ex);
                throw;
            }
            catch (Exception ex)
            {
                this._logger.Error((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), ex);
                throw new DataProviderApplicationException("SqlDataProvider. Erro durante criação da condição WHERE.", ex);
            }
        }

        public string FormatColumn(object Valor)
        {
            try
            {
                string str1 = "System.UInt16,System.UInt32,System.UInt64,System.Decimal,System.Byte,System.Int16,System.Int32,System.Int64,System.SByte,System.Single,System.Double";
                string str2 = "System.Char,System.String";
                string str3 = "System.DateTime,System.TimeSpan";
                return str1.IndexOf(Valor.GetType().ToString()) <= 0 ? (str2.IndexOf(Valor.GetType().ToString()) <= 0 ? (str3.IndexOf(Valor.GetType().ToString()) <= 0 ? "Null" : Valor.ToString()) : "'" + Valor.ToString().Replace("'", "''") + "'") : Valor.ToString();
            }
            catch (DataProviderApplicationException ex)
            {
                this._logger.Debug((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), (Exception)ex);
                throw;
            }
            catch (Exception ex)
            {
                this._logger.Error((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), ex);
                throw new DataProviderApplicationException("SqlDataProvider. Erro durante formatação das colunas.", ex);
            }
        }

        public object GetNullValue(object Valor)
        {
            try
            {
                object obj1 = (object)DBNull.Value;
                object obj2;
                switch (Valor.GetType().ToString())
                {
                    case "System.UInt16":
                        obj2 = (int)Convert.ToUInt16(Valor) == 0 ? (object)null : Valor;
                        break;
                    case "System.UInt32":
                        obj2 = (int)Convert.ToUInt32(Valor) == 0 ? (object)null : Valor;
                        break;
                    case "System.UInt64":
                        obj2 = (long)Convert.ToUInt64(Valor) == 0L ? (object)null : Valor;
                        break;
                    case "System.Decimal":
                        obj2 = Convert.ToDecimal(Valor) == new Decimal(-1, -1, -1, true, (byte)0) ? (object)null : Valor;
                        break;
                    case "System.Byte":
                        obj2 = (int)Convert.ToByte(Valor) == 0 ? (object)null : Valor;
                        break;
                    case "System.Int16":
                        obj2 = (int)Convert.ToInt16(Valor) == (int)short.MinValue ? (object)null : Valor;
                        break;
                    case "System.Int32":
                        obj2 = Convert.ToInt32(Valor) == int.MinValue ? (object)null : Valor;
                        break;
                    case "System.Int64":
                        obj2 = Convert.ToInt64(Valor) == long.MinValue ? (object)null : Valor;
                        break;
                    case "System.SByte":
                        obj2 = (int)Convert.ToSByte(Valor) == (int)sbyte.MinValue ? (object)null : Valor;
                        break;
                    case "System.Single":
                        obj2 = (double)Convert.ToSingle(Valor) == -3.40282346638529E+38 ? (object)null : Valor;
                        break;
                    case "System.Double":
                        obj2 = Convert.ToDouble(Valor) == double.MinValue ? (object)null : Valor;
                        break;
                    default:
                        obj2 = Valor;
                        break;
                }
                if (obj2 == null)
                    obj2 = (object)DBNull.Value;
                return obj2;
            }
            catch (DataProviderApplicationException ex)
            {
                this._logger.Debug((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), (Exception)ex);
                throw;
            }
            catch (Exception ex)
            {
                this._logger.Error((object)(MethodBase.GetCurrentMethod().Name + " - " + ex.Message), ex);
                throw new DataProviderApplicationException("SqlDataProvider. Ocorreu um erro no tratamento de valores nulos.", ex);
            }
        }

        private void Dispose(bool pDisposing)
        {
            if (this._disposed)
                return;
            this.CloseConnection();
            if (pDisposing)
            {
                if (this._transaction != null)
                    this._transaction = (SqlTransaction)null;
                if (this._logger != null)
                {
                    this._logger.Dispose();
                    this._logger = (Logger)null;
                }
            }
            this._disposed = true;
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize((object)this);
        }
    }
}
   
