﻿// 
//   SubSonic - http://subsonicproject.com
// 
//   The contents of this file are subject to the New BSD
//   License (the "License"); you may not use this file
//   except in compliance with the License. You may obtain a copy of
//   the License at http://www.opensource.org/licenses/bsd-license.php
//  
//   Software distributed under the License is distributed on an 
//   "AS IS" basis, WITHOUT WARRANTY OF ANY KIND, either express or
//   implied. See the License for the specific language governing
//   rights and limitations under the License.
// 
using System.Data;
using System.Text;
using SubSonic.Schema;
using System;
using System.Linq;
using System.Collections.Generic;
using SubSonic.DataProviders;
using SubSonic.Query;
using System.Data.SqlClient;

namespace SubSonic.SqlGeneration.Schema
{
    public class Sql2005Schema : ANSISchemaGenerator
    {
        public Sql2005Schema()
        {
            ADD_COLUMN = @"ALTER TABLE [{0}] ADD [{1}]{2};";
            ALTER_COLUMN = @"ALTER TABLE [{0}] ALTER COLUMN [{1}]{2};";
            CREATE_TABLE = "CREATE TABLE [{0}] ({1} \r\n);";
            DROP_COLUMN = @"ALTER TABLE [{0}] DROP COLUMN [{1}];";
            DROP_TABLE = @"DROP TABLE {0};";
            GET_DB_CONSTRAINTS =
@"SELECT OBJECT_NAME(OBJECT_ID) AS Name, SCHEMA_NAME(schema_id) AS SchemaName, OBJECT_NAME(parent_object_id) AS TableName, type_desc AS Type
FROM sys.objects
WHERE type_desc LIKE '%CONSTRAINT' AND OBJECT_NAME(OBJECT_ID) NOT LIKE 'PK__%'";
            GET_DB_COLUMN_DEFINITIONS =
@"SELECT 
	TABLE_CATALOG,
	TABLE_SCHEMA,
	TABLE_NAME,
	COLUMN_NAME,
	ORDINAL_POSITION,
	COLUMN_DEFAULT, 
	DATA_TYPE, 
	CHARACTER_MAXIMUM_LENGTH, 
	DATETIME_PRECISION AS DatePrecision, 
	CASE WHEN IS_NULLABLE ='NO' THEN 0 ELSE 1 END AS IsNullable,
    COLUMNPROPERTY(object_id('[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']'), COLUMN_NAME, 'IsIdentity') AS IsIdentity,
	COLUMNPROPERTY(object_id('[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']'), COLUMN_NAME, 'IsComputed') as IsComputed 
FROM INFORMATION_SCHEMA.COLUMNS ORDER BY TABLE_NAME, ORDINAL_POSITION ASC";

        }
        /// <summary>
        /// Get's a list of all contraints in the database that are not primary key contraints
        /// </summary>
        /// <param name="Provider"></param>
        /// <returns></returns>
        public override IEnumerable<IConstraint> GetConstraintsFromDB(IDataProvider Provider)
        {
            var Inline = new InlineQuery(Provider, GET_DB_CONSTRAINTS);

            constraints = Inline.ExecuteTypedList<DatabaseContraint>()
                .Select((X) =>
                    (IConstraint)X)
                .ToList();

            return constraints;
        }

        public override IEnumerable<IColumnDefinition> GetColumnDefinitionsFromDB(IDataProvider Provider)
        {
            var Inline = new InlineQuery(Provider, GET_DB_COLUMN_DEFINITIONS);

            columnDefinitions = Inline.ExecuteTypedList<DBColumnDefinition>()
                .Select((X) =>
                    (IColumnDefinition)X)
                .ToList();

            return columnDefinitions;
        }

        /// <summary>
        /// Removes the column.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public override string BuildDropColumnStatement(string tableName, string columnName)
        {
            StringBuilder sql = new StringBuilder();

            string defConstraint = string.Format("DF_{0}_{1}", tableName, columnName);

            sql.AppendFormat("IF  EXISTS (SELECT * FROM dbo.sysobjects WHERE id = OBJECT_ID(N'[{0}]') AND type = 'D')\r\n",
                defConstraint);
            sql.AppendLine("BEGIN");
            sql.AppendFormat("ALTER TABLE {0} DROP CONSTRAINT [{1}]\r\n", tableName, defConstraint);
            sql.AppendLine("END");

            //if this is a PK we'll need to drop that too

            //check to see if there are any constraints
            //QueryCommand cmd;
            //if (column.DefaultSetting != null) {
            //    sql.AppendFormat("ALTER TABLE {0} DROP CONSTRAINT DF_{0}_{1}", tableName, columnName);
            //    sql.Append(";\r\n");

            //    //drop FK constraints ...

            //    //drop CHECK constraints ...
            //}

            sql.AppendFormat(DROP_COLUMN, tableName, columnName);
            return sql.ToString();
        }

        public override string BuildCreateTableStatement(ITable table, bool includeComputedColumns)
        {
            StringBuilder oSql = new StringBuilder(base.BuildCreateTableStatement(table, includeComputedColumns));

            List<IColumn> PrimaryKeys = table.Columns.Where((X) => X.IsPrimaryKey).ToList();

            //add a named PK constraint so we can drop it later
            oSql.AppendLine();
            oSql.AppendFormat("ALTER TABLE {0} ADD CONSTRAINT [PK_{1}_{2}] PRIMARY KEY({3})", table.QualifiedName, table.Name, String.Join("_", PrimaryKeys.Select((X) => X.Name).ToArray()), String.Join(", ", PrimaryKeys.Select((X) => String.Format("[{0}]", X.Name)).ToArray()));
            oSql.AppendLine();

            return oSql.ToString();
        }

        public override string GetNativeType(DbType dbType)
        {
            switch (dbType)
            {
                case DbType.Object:
                    return "varbinary";
                case DbType.AnsiString:
                case DbType.AnsiStringFixedLength:
                case DbType.String:
                case DbType.StringFixedLength:
                    return "nvarchar";
                case DbType.Boolean:
                    return "bit";
                case DbType.SByte:
					return "image";
                case DbType.Binary:
                    return "binary";
                case DbType.Byte:
                    return "tinyint";
                case DbType.Currency:
                    return "money";
                case DbType.Time:
                case DbType.Date:
                case DbType.DateTime:
                    return "datetime";
                case DbType.Decimal:
                    return "decimal";
                case DbType.Double:
                    return "float";
                case DbType.Guid:
                    return "uniqueidentifier";
                case DbType.Int16:
                case DbType.UInt16:
                    return "smallint";
                case DbType.Int32:
                case DbType.UInt32:
                    return "int";
                case DbType.UInt64:
                case DbType.Int64:
                    return "bigint";
                case DbType.Single:
                    return "real";
                case DbType.VarNumeric:
                    return "numeric";
                case DbType.Xml:
                    return "xml";
                default:
                    return "nvarchar";
            }
        }

        /// <summary>
        /// Sets the column attributes.
        /// </summary>
        /// <param name="Column">The column.</param>
        /// <returns></returns>
        public override string GenerateColumnAttributes(IColumn Column, bool exist)
        {
            StringBuilder sb = new StringBuilder();
            if (Column.DataType == DbType.String && Column.MaxLength > 8000)
            {
                //use nvarchar MAX 
                //TODO - this won't work for SQL 2000
                //need to tell the diff somehow
                sb.Append(" nvarchar(MAX)");
            }
            else
            {
                sb.Append(" " + GetNativeType(Column.DataType));

                if (Column.MaxLength > 0)
                    sb.Append("(" + Column.MaxLength + ")");

                if (Column.DataType == DbType.Decimal)
                    sb.Append("(" + Column.NumericPrecision + ", " + Column.NumberScale + ")");
            }

            if (Column.IsPrimaryKey | !Column.IsNullable)
                sb.Append(" NOT NULL");

            if (!exist && (Column.IsPrimaryKey && Column.IsNumeric && Column.AutoIncrement))
                sb.Append(" IDENTITY(1,1)");
            else if (!exist && (Column.IsPrimaryKey && Column.DataType == DbType.Guid))
                Column.DefaultSetting = "NEWID()";


            if (!exist && Column.DefaultSetting != null)
            {
                sb.Append(" CONSTRAINT DF_" + Column.Table.Name + "_" + Column.Name + " DEFAULT (" +
                          GetDefaultValue(Column).ToString() + ")");
            }

            return sb.ToString();
        }

        public override object GetDefaultValue(IColumn Column)
        {
            object oResult = null;

            if (Column.DefaultSetting != null)
            {
                Type oType = Column.DefaultSetting.GetType();

                if (oType == typeof(string) || oType == typeof(DateTime))
                {
                    if (!Column.DefaultSetting.ToString().EndsWith("()"))
                    {
                        oResult = String.Format("'{0}'", Column.DefaultSetting);
                    }
                    else
                    {
                        oResult = Column.DefaultSetting;
                    }
                }
                else if (oType == typeof(bool))
                {
                    oResult = (bool)Column.DefaultSetting ? 1 : 0;
                }
                else
                {
                    oResult = Column.DefaultSetting;
                }
            }

            return oResult;
        }

        /// <summary>
        /// Gets the type of the db.
        /// </summary>
        /// <param name="sqlType">Type of the SQL.</param>
        /// <returns></returns>
        public override DbType GetDbType(string sqlType)
        {
            switch (sqlType)
            {
                case "varchar":
                    return DbType.AnsiString;
                case "nvarchar":
                    return DbType.String;
                case "int":
                    return DbType.Int32;
                case "uniqueidentifier":
                    return DbType.Guid;
                case "datetime":
                case "datetime2":
                    return DbType.DateTime;
                case "bigint":
                    return DbType.Int64;
                case "binary":
                    return DbType.Binary;
                case "bit":
                    return DbType.Boolean;
                case "char":
                    return DbType.AnsiStringFixedLength;
                case "decimal":
                    return DbType.Decimal;
                case "float":
                    return DbType.Double;
                case "image":
                    return DbType.Binary;
                case "money":
                    return DbType.Currency;
                case "nchar":
                    return DbType.String;
                case "ntext":
                    return DbType.String;
                case "numeric":
                    return DbType.Decimal;
                case "real":
                    return DbType.Single;
                case "smalldatetime":
                    return DbType.DateTime;
                case "smallint":
                    return DbType.Int16;
                case "smallmoney":
                    return DbType.Currency;
                case "sql_variant":
                    return DbType.String;
                case "sysname":
                    return DbType.String;
                case "text":
                    return DbType.AnsiString;
                case "timestamp":
                    return DbType.Binary;
                case "tinyint":
                    return DbType.Byte;
                case "varbinary":
                    return DbType.Binary;
                default:
                    return DbType.AnsiString;
            }
        }
    }
}