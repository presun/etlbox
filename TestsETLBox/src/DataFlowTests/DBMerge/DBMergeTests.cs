using ALE.ETLBox;
using ALE.ETLBox.ConnectionManager;
using ALE.ETLBox.ControlFlow;
using ALE.ETLBox.DataFlow;
using ALE.ETLBox.Helper;
using ALE.ETLBox.Logging;
using ALE.ETLBoxTests.Fixtures;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace ALE.ETLBoxTests.DataFlowTests
{
    [Collection("DataFlow")]
    public class DBMergeTests
    {
        public static IEnumerable<object[]> Connections => Config.AllSqlConnections("DataFlow");

        public DBMergeTests(DataFlowDatabaseFixture dbFixture)
        {
        }

        public class MyMergeRow : MergeableRow
        {
            [IdColumn]
            [ColumnMap("Col1")]
            public long Key { get; set; }

            [CompareColumn]
            [ColumnMap("Col2")]
            public string Value { get; set; }
        }

        [Theory, MemberData(nameof(Connections))]
        public void SimpleMerge(IConnectionManager connection)
        {
            //Arrange
            TwoColumnsTableFixture s2c = new TwoColumnsTableFixture(connection, "DBMergeSource");
            s2c.InsertTestData();
            s2c.InsertTestDataSet2();
            TwoColumnsTableFixture d2c = new TwoColumnsTableFixture(connection, "DBMergeDestination");
            d2c.InsertTestDataSet3();
            DBSource<MyMergeRow> source = new DBSource<MyMergeRow>(connection, "DBMergeSource");

            //Act
            DBMerge<MyMergeRow> dest = new DBMerge<MyMergeRow>(connection, "DBMergeDestination");
            source.LinkTo(dest);
            source.Execute();
            dest.Wait();

            //Assert
            Assert.Equal(6, RowCountTask.Count(connection, "DBMergeDestination", $"{d2c.QB}Col1{d2c.QE} BETWEEN 1 AND 7 AND {d2c.QB}Col2{d2c.QE} LIKE 'Test%'"));
            Assert.True(dest.DeltaTable.Count == 7);
            Assert.True(dest.DeltaTable.Where(row => row.ChangeAction == "U").Count() == 2);
            Assert.True(dest.DeltaTable.Where(row => row.ChangeAction == "D" && row.Key == 10).Count() == 1);
            Assert.True(dest.DeltaTable.Where(row => row.ChangeAction == "I").Count() == 3);
            Assert.True(dest.DeltaTable.Where(row => row.ChangeAction == "E" && row.Key == 1).Count() == 1);
        }

        [Theory, MemberData(nameof(Connections))]
        public void DisablingDeletion(IConnectionManager connection)
        {
            //Arrange
            TwoColumnsTableFixture s2c = new TwoColumnsTableFixture(connection, "DBMergeSource");
            s2c.InsertTestData();
            TwoColumnsTableFixture d2c = new TwoColumnsTableFixture(connection, "DBMergeDestination");
            d2c.InsertTestDataSet3();
            DBSource<MyMergeRow> source = new DBSource<MyMergeRow>(connection, "DBMergeSource");

            //Act
            DBMerge<MyMergeRow> dest = new DBMerge<MyMergeRow>(connection, "DBMergeDestination");
            dest.DisableDeletion = true;
            source.LinkTo(dest);
            source.Execute();
            dest.Wait();

            //Assert
            Assert.Equal(5, RowCountTask.Count(connection, "DBMergeDestination"));
            Assert.True(dest.DeltaTable.Count == 3);
            Assert.True(dest.DeltaTable.Where(row => row.ChangeAction == "I" && row.Key == 3).Count() == 1);
            Assert.True(dest.DeltaTable.Where(row => row.ChangeAction == "U" && row.Key == 2).Count() == 1);
            Assert.True(dest.DeltaTable.Where(row => row.ChangeAction == "E" && row.Key == 1).Count() == 1);
        }

        [Theory, MemberData(nameof(Connections))]
        public void EnforcingTruncate(IConnectionManager connection)
        {
            //Arrange
            TwoColumnsTableFixture s2c = new TwoColumnsTableFixture(connection, "DBMergeSource");
            s2c.InsertTestData();
            TwoColumnsTableFixture d2c = new TwoColumnsTableFixture(connection, "DBMergeDestination");
            d2c.InsertTestDataSet3();
            DBSource<MyMergeRow> source = new DBSource<MyMergeRow>(connection, "DBMergeSource");

            //Act
            DBMerge<MyMergeRow> dest = new DBMerge<MyMergeRow>(connection, "DBMergeDestination");
            dest.UseTruncateMethod = true;
            source.LinkTo(dest);
            source.Execute();
            dest.Wait();

            //Assert
            Assert.Equal(3, RowCountTask.Count(connection, "DBMergeDestination"));
            Assert.True(dest.DeltaTable.Count == 5);
            Assert.True(dest.DeltaTable.Where(row => row.ChangeAction == "E" && row.Key == 1).Count() == 1);
            Assert.True(dest.DeltaTable.Where(row => row.ChangeAction == "U" && row.Key == 2).Count() == 1);
            Assert.True(dest.DeltaTable.Where(row => row.ChangeAction == "I" && row.Key == 3).Count() == 1);
            Assert.True(dest.DeltaTable.Where(row => row.ChangeAction == "D" && row.Key == 4).Count() == 1);
            Assert.True(dest.DeltaTable.Where(row => row.ChangeAction == "D" && row.Key == 10).Count() == 1);
        }
    }
}
