// 
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
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Linq;
using SubSonic.Extensions;
using SubSonic.DataProviders;
using SubSonic.Schema;

namespace SubSonic.SqlGeneration.Schema
{
    /// <summary>
    /// A schema generator for your DB
    /// </summary>
    public abstract class ANSISchemaGenerator : ISchemaGenerator
    {
        protected string ADD_COLUMN = @"ALTER TABLE {0} ADD {1}{2};";
        protected string ADD_DEFAULT_CONSTRAINT = @"ALTER TABLE [{0}] ADD CONSTRAINT [DF_{0}_{1}] DEFAULT ({2}) FOR [{1}]";
        protected string ADD_UNIQUE_CONSTRAINT = @"ALTER TABLE [{0}] ADD CONSTRAINT [UNIQUE_{0}_{1}] UNIQUE({2})";
        protected string ADD_FOREIGN_KEY_CONSTRAINT = @"ALTER TABLE [{0}] ADD CONSTRAINT [FK_{0}_{1}] FOREIGN KEY({2}) REFERENCES [{1}]({3})";
        protected string ALTER_COLUMN = @"ALTER TABLE {0} ALTER COLUMN {1}{2};";
        protected string CREATE_TABLE = "CREATE TABLE {0} ({1} \r\n);";
        protected string DROP_COLUMN = @"ALTER TABLE {0} DROP COLUMN {1};";
        protected string DROP_TABLE = @"DROP TABLE {0};";
        protected string DROP_CONSTRAINT = @"ALTER TABLE [{0}].[{1}] DROP CONSTRAINT [{2}]";
        protected string GET_DB_CONSTRAINTS = "";
        protected string GET_DB_COLUMN_DEFINITIONS = "";
        protected List<IConstraint> constraints = null;
        protected List<IColumnDefinition> columnDefinitions = null;

        public abstract IEnumerable<IConstraint> GetConstraintsFromDB(IDataProvider Provider);
        public abstract IEnumerable<IColumnDefinition> GetColumnDefinitionsFromDB(IDataProvider Provider);

        public virtual IConstraint GetDefaultConstraintForColumn(IColumn Column)
        {
            return Constraints.SingleOrDefault((X) => X.TableName == Column.Table.Name && X.Name == String.Format("DF_{0}_{1}", Column.Table.Name, Column.Name));
        }

        public IEnumerable<IConstraint> Constraints
        {
            get
            {
                if (constraints == null)
                {
                    constraints = GetConstraintsFromDB(ProviderFactory.GetProvider()).ToList();
                }

                return constraints;
            }
        }

        public IEnumerable<IColumnDefinition> ColumnDefinitions
        {
            get
            {
                if (columnDefinitions == null)
                {
                    columnDefinitions = GetColumnDefinitionsFromDB(ProviderFactory.GetProvider()).ToList();
                }

                return columnDefinitions;
            }
        }

        #region ISchemaGenerator Members

        /// <summary>
        /// Builds a CREATE TABLE statement.
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public virtual string BuildCreateTableStatement(ITable table, bool includeComputedColumns)
        {
            string columnSql = GenerateColumns(table, includeComputedColumns);
            return string.Format(CREATE_TABLE, table.Name, columnSql);
        }

        public virtual string BuildTableConstraintStatement(ITable Table)
        {
            StringBuilder oSql = new StringBuilder();

            List<IColumn> ForeignKeyColumns = Table.Columns.Where((X) => X.IsForeignKey).ToList();
            List<IColumn> UniqueColumns = Table.Columns.Where((X) => X.IsUnique).ToList();

            foreach (IColumn Column in ForeignKeyColumns)
            {   /// apply foreign key constraints
                oSql.AppendLine(String.Format(ADD_FOREIGN_KEY_CONSTRAINT, Table, Column.ForeignKeyTo, Table.Columns.Single((X) => X.Name == Column.ForeignKeyTo.PrimaryKey.Name), Column.ForeignKeyTo.PrimaryKey));
            }
            
            if (UniqueColumns.Count > 0)
            {   /// apply unique constraint
                oSql.AppendLine(String.Format(ADD_UNIQUE_CONSTRAINT, Table, String.Join("_", UniqueColumns.Select((X) => X.Name).ToArray()), String.Join(", ", UniqueColumns.Select((X) => X.Name).ToArray())));
            }

            return oSql.ToString();
        }

        /// <summary>
        /// Builds a DROP TABLE statement.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <returns></returns>
        public virtual string BuildDropTableStatement(string tableName)
        {
            return string.Format(DROP_TABLE, tableName);
        }

        public virtual bool BuildDropDBConstraintStatement(IDataProvider Provider, ref string Sql)
        {   /// Get the list of constraints
            IEnumerable<IConstraint> Constraints = GetConstraintsFromDB(Provider);
            ///determine that we have constraints
            Boolean hasConstraints = Constraints.Count() > 0;

            if (hasConstraints)
            {
                try
                {
                    StringBuilder oSql = new StringBuilder();

                    foreach (IConstraint Constraint in Constraints)
                    {
                        oSql.AppendLine(String.Format(DROP_CONSTRAINT, Constraint.SchemaName, Constraint.TableName, Constraint.Name));
                    }

                    Sql = oSql.ToString();
                }
                catch
                {
                    hasConstraints = false;
                    Sql = String.Empty;
                }
            }

            return hasConstraints;
        }

        /// <summary>
        /// Adds the column.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="column">The column.</param>
        /// <returns></returns>
        public virtual string BuildAddColumnStatement(string tableName, IColumn column)
        {
            var sql = new StringBuilder();

            //if we're adding a Non-null column to the DB schema, there has to be a default value
            //otherwise it will result in an error'
            if (!column.IsNullable && column.DefaultSetting == null)
            {
                SetColumnDefaults(column);
            }

            sql.AppendFormat(ADD_COLUMN, tableName, column.Name, GenerateColumnAttributes(column, false));
            
            return sql.ToString();
        }

        public virtual string BuildDefaultConstraintStatement(IColumn Column)
        {
            StringBuilder oSql = new StringBuilder();

            if (Column.Table == null)
            {
                throw new InvalidOperationException("column must be a part of a defined table.");
            }

            if (Column.DefaultSetting != null)
            {
                oSql.AppendFormat(ADD_DEFAULT_CONSTRAINT, Column.Table.Name, Column.Name, Column.DefaultValue);
            }

            return oSql.ToString();
        }

        /// <summary>
        /// Alters the column.
        /// </summary>
        /// <param name="column">The column.</param>
        public virtual string BuildAlterColumnStatement(IColumn column)
        {
            var sql = new StringBuilder();
            sql.AppendFormat(ALTER_COLUMN, column.Table.Name, column.Name, GenerateColumnAttributes(column, column.Provider.GetTableFromDB(column.Table.Name) != null));
            return sql.ToString();
        }

        /// <summary>
        /// Removes the column.
        /// </summary>
        /// <param name="tableName">Name of the table.</param>
        /// <param name="columnName">Name of the column.</param>
        /// <returns></returns>
        public virtual string BuildDropColumnStatement(string tableName, string columnName)
        {
            StringBuilder sql = new StringBuilder();
            sql.AppendFormat(DROP_COLUMN, tableName, columnName);
            return sql.ToString();
        }

        /// <summary>
        /// Gets the type of the native.
        /// </summary>
        /// <param name="dbType">Type of the db.</param>
        /// <returns></returns>
        public abstract string GetNativeType(DbType dbType);

        /// <summary>
        /// Generates the columns.
        /// </summary>
        /// <param name="table">Table containing the columns.</param>
        /// <returns>
        /// SQL fragment representing the supplied columns.
        /// </returns>
        public virtual string GenerateColumns(ITable table, bool includeComputedColumns)
        {
            StringBuilder createSql = new StringBuilder();

            foreach (IColumn col in table.Columns.Where((X) => (includeComputedColumns && (X.IsComputed || !X.IsComputed)) || !X.IsComputed))
                createSql.AppendFormat("\r\n  [{0}]{1},", col.Name, GenerateColumnAttributes(col, table.Provider.GetTableFromDB(table.Name) != null));
            string columnSql = createSql.ToString();
            return columnSql.Chop(",");
        }

        /// <summary>
        /// Sets the column attributes.
        /// </summary>
        /// <param name="column">The column.</param>
        /// <returns></returns>
        public abstract string GenerateColumnAttributes(IColumn column, bool exist);

        /// <summary>
        /// Get's the default value from the default column setting
        /// </summary>
        /// <param name="Column"></param>
        /// <returns></returns>
        public abstract object GetDefaultValue(IColumn Column);

        ///<summary>
        ///Gets an ITable from the DB based on name
        ///</summary>
        public virtual ITable GetTableFromDB(IDataProvider provider, string tableName)
        {
            ITable result = null;
            DataTable schema;

            using(var scope = new AutomaticConnectionScope(provider))
            {
                var restrictions = new string[4] {null, null, tableName, null};
                schema = scope.Connection.GetSchema("COLUMNS", restrictions);
            }

            if(schema.Rows.Count > 0)
            {
                result = new DatabaseTable(tableName, provider);
                foreach(DataRow dr in schema.Rows)
                {
                    IColumn col = new DatabaseColumn(dr["COLUMN_NAME"].ToString(), result);
                    col.DataType = GetDbType(dr["DATA_TYPE"].ToString());
                    col.IsNullable = dr["IS_NULLABLE"].ToString() == "YES";

                    string maxLength = dr["CHARACTER_MAXIMUM_LENGTH"].ToString();

                    int iMax = 0;
                    int.TryParse(maxLength, out iMax);
                    col.MaxLength = iMax;
                    result.Columns.Add(col);
                }
            }

            return result;
        }

        /// <summary>
        /// Creates a list of table names
        /// </summary>
        public virtual string[] GetTableList(IDataProvider provider)
        {
            List<string> result = new List<string>();
            using(DbConnection conn = provider.CreateConnection())
            {
                conn.Open();
                var schema = conn.GetSchema("TABLES");
                conn.Close();

                foreach(DataRow dr in schema.Rows)
                {
                    if(dr["TABLE_TYPE"].ToString().Equals("BASE TABLE"))
                        result.Add(dr["TABLE_NAME"].ToString());
                }
            }
            return result.ToArray();
        }

        public abstract DbType GetDbType(string sqlType);

        #endregion


        public virtual void SetColumnDefaults(IColumn column)
        {
            if(column.IsNumeric)
                column.DefaultSetting = 0;
            else if(column.IsDateTime)
                column.DefaultSetting = DateTime.Parse("1/1/1900");
            else if(column.IsString)
                column.DefaultSetting = "";
            else if(column.DataType == DbType.Boolean)
                column.DefaultSetting = 0;
        }
    }
}