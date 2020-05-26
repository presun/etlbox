using ALE.ETLBox;
using ALE.ETLBox.ConnectionManager;
using ALE.ETLBox.ControlFlow;
using ALE.ETLBox.DataFlow;
using ALE.ETLBox.Helper;
using ALE.ETLBox.Logging;
using ALE.ETLBoxTests.Fixtures;
using System;
using System.Collections.Generic;
using System.Dynamic;
using Xunit;

namespace ALE.ETLBoxTests.DataFlowTests
{
    [Collection("DataFlow")]
    public class ExcelSourceStringArrayTests
    {
        public SqlConnectionManager Connection => Config.SqlConnection.ConnectionManager("DataFlow");
        public ExcelSourceStringArrayTests(DataFlowDatabaseFixture dbFixture)
        {
        }

        public class MyData
        {
            public int Col1 { get; set; }
            public string Col2 { get; set; }
        }

        [Fact]
        public void SimpleDataNoHeader()
        {
            //Arrange
            TwoColumnsTableFixture dest2Columns = new TwoColumnsTableFixture("ExcelDestinationStringArray");

            //Act
            ExcelSource<string[]> source = new ExcelSource<string[]>("res/Excel/TwoColumnData.xlsx")
            {
                HasNoHeader = true
            };
            RowTransformation<string[], MyData> trans = new RowTransformation<string[], MyData>(row =>
            {
                MyData result = new MyData();
                result.Col1 = int.Parse(row[0]);
                result.Col2 = row[1];
                return result;
            });
            DbDestination<MyData> dest = new DbDestination<MyData>(Connection, "ExcelDestinationStringArray");

            source.LinkTo(trans);
            trans.LinkTo(dest);
            source.Execute();
            dest.Wait();

            //Assert
            dest2Columns.AssertTestData();
        }
    }
}
