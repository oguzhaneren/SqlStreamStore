namespace SqlStreamStore
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.IO;
    using System.Threading.Tasks;
    using SqlStreamStore.Connection;
    using SqlStreamStore.Infrastructure;

    public class MsSqlStreamStoreFixture : StreamStoreAcceptanceTestFixture
    {
        public readonly string ConnectionString;
        private readonly string _schema;
        private readonly string _databaseName;
        private readonly ILocalInstance _localInstance;

        public MsSqlStreamStoreFixture(string schema)
        {
            _schema = schema;
            _localInstance = new LocalInstance();

            //var uniqueName = Guid.NewGuid().ToString().Replace("-", string.Empty);
            var uniqueName = Path.GetRandomFileName().Replace(".", string.Empty);
            _databaseName = $"StreamStoreTests-{uniqueName}";

            ConnectionString = CreateConnectionString();
        }

        public override long MinPosition => 0;

        public override async Task<IStreamStore> GetStreamStore()
        {
            await CreateDatabase();

            return await GetStreamStore(_schema);
        }

       
        Tuple<SqlConnection, SqlTransaction> _connectionTuple = null;
        private object _lock = new object();

        public async Task<IStreamStore> GetStreamStore(string schema)
        {
            var factory = new ExternalyManagedDatabaseSessionFactory(() =>
            {
                if (_connectionTuple == null)
                {
                    lock (_lock)
                    {
                        if (_connectionTuple == null)
                        {
                            var sqlConnection = new SqlConnection(ConnectionString);
                            sqlConnection.Open();
                            var tx = sqlConnection.BeginTransaction();

                            _connectionTuple = Tuple.Create(sqlConnection, tx);
                        }
                    }
                }

                return Task.FromResult(_connectionTuple);
            });

            var settings = new MsSqlStreamStoreSettings(ConnectionString)
            {
                Schema = schema,
                GetUtcNow = () => GetUtcNow(),
                Factory = factory
            };
            var store = new MsSqlStreamStore(settings);
            await store.CreateSchema();

            return store;
        }

        public async Task<MsSqlStreamStore> GetStreamStore_v1Schema()
        {
            await CreateDatabase();
            var settings = new MsSqlStreamStoreSettings(ConnectionString)
            {
                Schema = _schema,
                GetUtcNow = () => GetUtcNow()
            };
            var store = new MsSqlStreamStore(settings);
            await store.CreateSchema_v1_ForTests();

            return store;
        }

        public async Task<MsSqlStreamStore> GetUninitializedStreamStore()
        {
            await CreateDatabase();

            return new MsSqlStreamStore(new MsSqlStreamStoreSettings(ConnectionString)
            {
                Schema = _schema,
                GetUtcNow = () => GetUtcNow()
            });
        }

        public async Task<MsSqlStreamStore> GetMsSqlStreamStore()
        {
            await CreateDatabase();

            var settings = new MsSqlStreamStoreSettings(ConnectionString)
            {
                Schema = _schema,
                GetUtcNow = () => GetUtcNow(),

            };

            var store = new MsSqlStreamStore(settings);
            await store.CreateSchema();

            return store;
        }

        public override void Commit()
        {
            _connectionTuple?.Item2.Commit();
            _connectionTuple?.Item2.Dispose();
            _connectionTuple?.Item1.Dispose();
        }

        public override void Dispose()
        {
            if(_connectionTuple?.Item1.State == ConnectionState.Open)
            {
                //_connectionTuple?.Item2.Commit();
                _connectionTuple?.Item2.Dispose();
                _connectionTuple?.Item1.Dispose();
            }
           

            using (var sqlConnection = new SqlConnection(ConnectionString))
            {
                // Fixes: "Cannot drop database because it is currently in use"
                SqlConnection.ClearPool(sqlConnection);
            }

            using (var connection = _localInstance.CreateConnection())
            {
                connection.Open();
                using (var command = new SqlCommand($"ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE", connection))
                {
                    command.ExecuteNonQuery();
                }
                using (var command = new SqlCommand($"DROP DATABASE [{_databaseName}]", connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        private async Task CreateDatabase()
        {
            using (var connection = _localInstance.CreateConnection())
            {
                await connection.OpenAsync().NotOnCapturedContext();
                var tempPath = Environment.GetEnvironmentVariable("Temp");
                var createDatabase = $"CREATE DATABASE [{_databaseName}] on (name='{_databaseName}', "
                                     + $"filename='{tempPath}\\{_databaseName}.mdf')";
                using (var command = new SqlCommand(createDatabase, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        private string CreateConnectionString()
        {
            var connectionStringBuilder = _localInstance.CreateConnectionStringBuilder();
            connectionStringBuilder.MultipleActiveResultSets = true;
            connectionStringBuilder.IntegratedSecurity = true;
            connectionStringBuilder.InitialCatalog = _databaseName;

            return connectionStringBuilder.ToString();
        }

     

        private interface ILocalInstance
        {
            SqlConnection CreateConnection();
            SqlConnectionStringBuilder CreateConnectionStringBuilder();
        }

        private class LocalInstance : ILocalInstance
        {
            private readonly string connectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=master;Integrated Security=SSPI;";

            public SqlConnection CreateConnection()
            {
                return new SqlConnection(connectionString);
            }

            public SqlConnectionStringBuilder CreateConnectionStringBuilder()
            {
                return new SqlConnectionStringBuilder(connectionString);
            }
        }
    }
}