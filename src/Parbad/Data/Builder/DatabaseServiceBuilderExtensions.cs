// Copyright (c) Parbad. All rights reserved.
// Licensed under the GNU GENERAL PUBLIC License, Version 3.0. See License.txt in the project root for license information.

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Parbad.Data.Context;
using Parbad.Data.Initializers;
using Parbad.Internal;

namespace Parbad.Builder
{
    public static class DatabaseServiceBuilderExtensions
    {
        /// <summary>
        /// Configures the database required by Parbad to save and load payments and transactions data.
        /// <para>
        /// Note: It uses Microsoft.EntityFrameworkCore as storage, which means you can save your data
        /// in different database providers such as SQL Server, MySql, Sqlite, PostgreSQL, Oracle, InMemory, etc.
        /// For more information see: https://docs.microsoft.com/en-us/ef/core/providers/.
        /// </para>
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configureDatabaseBuilder">Configure what database must be used and how it must be created.</param>
        public static IParbadBuilder ConfigureDatabase(this IParbadBuilder builder,
            Action<DbContextOptionsBuilder> configureDatabaseBuilder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (configureDatabaseBuilder == null) throw new ArgumentNullException(nameof(configureDatabaseBuilder));

            builder.Services.AddDbContext<ParbadDataContext>(configureDatabaseBuilder);

            return builder;
        }

        /// <summary>
        /// Configures Parbad database initializer.
        /// Initializer is useful for creating, deleting, migrating and seeding the Parbad database.
        /// <para>Note: Use ConfigureDatabase method before using this method.</para>
        /// <para>Note: Use one of predefined database creators depends on the database
        /// provider (In-Memory, SQL Server, MySQL, Sqlite, etc.) that you chose.</para>
        /// <para>Providers examples:</para>
        /// <para>For In-Memory, use the CreateDatabase method.</para>
        /// <para>For SQL Server, use the CreateAndMigrateDatabase method.</para>
        /// <para>
        /// For Sqlite, use DeleteAndCreateDatabase.
        /// Because Sqlite has some limitations with migrations.
        /// In this case, one way to fix this is to first delete the old Parbad database and then create it again.
        /// </para>
        /// <para>
        /// If you prefer to initialize the Parbad database yourself, then use the UseInitializer method
        /// and define how the database must be initialized.
        /// </para>
        /// <para>Note: Initializers will be called in order that you specified.</para>
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configureInitializer"></param>
        public static IParbadBuilder ConfigureDatabaseInitializer(
            this IParbadBuilder builder,
            Action<IDatabaseInitializerBuilder> configureInitializer)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (configureInitializer == null) throw new ArgumentNullException(nameof(configureInitializer));

            configureInitializer(new DatabaseInitializerBuilder(builder.Services));

            var services = builder.Services.BuildServiceProvider();

            var database = services.GetRequiredService<ParbadDataContext>();
            var initializers = services.GetServices<IDatabaseInitializer>();

            foreach (var initializer in initializers)
            {
                initializer
                    .InitializeAsync(database)
                    .ConfigureAwaitFalse()
                    .GetAwaiter()
                    .GetResult();
            }

            return builder;
        }

        /// <summary>
        /// Uses the MigrationsAssembly method internally and passes the Parbad assembly name
        /// as parameter for configuring the migrations.
        /// </summary>
        /// <typeparam name="TBuilder"></typeparam>
        /// <typeparam name="TExtension"></typeparam>
        /// <param name="builder"></param>
        public static RelationalDbContextOptionsBuilder<TBuilder, TExtension> UseParbadMigrations<TBuilder, TExtension>(
            this RelationalDbContextOptionsBuilder<TBuilder, TExtension> builder)
            where TBuilder : RelationalDbContextOptionsBuilder<TBuilder, TExtension>
            where TExtension : RelationalOptionsExtension, new()
        {
            return builder.MigrationsAssembly("Parbad");
        }

        /// <summary>
        /// Configures the storage required by Parbad to save and load the data.
        /// Note: It uses Microsoft.EntityFrameworkCore as storage, which means you can save your data
        /// in different database providers such as SQL Server, MySql, Sqlite, PostgreSQL, Oracle, InMemory, etc.
        /// <para>For more information see: https://docs.microsoft.com/en-us/ef/core/providers/.</para>
        /// <para>Note: This method is deprecated. Use ConfigureDatabase instead.</para>
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="storageBuilder"></param>
        [Obsolete("Use ConfigureDatabase method instead.")]
        public static IParbadBuilder ConfigureStorage(this IParbadBuilder builder, Action<DbContextOptionsBuilder> storageBuilder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (storageBuilder == null) throw new ArgumentNullException(nameof(storageBuilder));

            return builder.ConfigureDatabase(storageBuilder);
        }

        /// <summary>
        /// Configures the context to connect to a Microsoft SQL Server database.
        /// It also creates and migrates the database.
        /// <para>Note: This method is deprecated. Use ConfigureDatabase and ConfigureDatabaseInitializer instead.</para>
        /// </summary>
        /// <param name="builder">The builder being used to configure the context.</param>
        /// <param name="connectionString">The connection string of the database to connect to.</param>
        [Obsolete("Use ConfigureDatabase and ConfigureDatabaseInitializer instead.")]
        public static DbContextOptionsBuilder UseParbadSqlServer(this DbContextOptionsBuilder builder,
            string connectionString)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (connectionString == null) throw new ArgumentNullException(nameof(connectionString));

            builder.UseSqlServer(connectionString, optionsBuilder => optionsBuilder.UseParbadMigrations());

            return builder;
        }
    }
}
