using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SubSonic.Schema;

namespace SubSonic.Query
{
    public class FromQuery
        : ITable
    {
        //private SqlQuery _Query = null;
        private QueryCommand _Cmd = null;

        #region constructors
        public FromQuery(SqlQuery oQuery, String sName)
            : base()
        {
            _Cmd = oQuery.GetCommand();
            Name = sName;
            Provider = _Cmd.Provider;
            
            Columns = oQuery.SelectColumns.Select((X) => 
                (IColumn)new DatabaseColumn(X.Name, this)
                {
                    DataType = X.DataType,
                    DefaultSetting = X.DefaultSetting,
                    ForeignKeyTo = X.ForeignKeyTo,
                    IsForeignKey = X.IsForeignKey,
                    FriendlyName = X.FriendlyName
                }).ToList();

            foreach (Aggregate oAggregate in oQuery.Aggregates)
            {
                bool hasAlais = !String.IsNullOrEmpty(oAggregate.Alias);

                Columns.Add(new DatabaseColumn(hasAlais ? oAggregate.Alias : oAggregate.ColumnName, this)
                {
                    DataType = oAggregate.GetDataType()
                });
            }
        }
        #endregion

        #region properties
        public string Name { get; set; }

        //public SqlQuery Query { get { return _query; } }

        public IList<IColumn> Columns { get; set; }

        public string ClassName
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public bool HasPrimaryKey
        {
            get { return false; }
        }

        public IColumn PrimaryKey
        {
            get { return null; }
        }

        public IColumn Descriptor
        {
            get { throw new NotImplementedException(); }
        }

        public string CreateSql
        {
            get { throw new NotImplementedException(); }
        }

        public string ConstraintsSql
        {
            get { throw new NotImplementedException(); }
        }

        public string DropSql
        {
            get { throw new NotImplementedException(); }
        }

        public string FriendlyName
        {
            get
            {
                return Name;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public string QualifiedName
        {
            get { return String.Format("({0})[{1}]", _Cmd.CommandSql, Name); }
        }

        public string SchemaName
        {
            get
            {
                return String.Empty;
            }
            set { }
        }
        #endregion
        
        public IColumn GetColumn(string columName)
        {
            return Columns.SingleOrDefault((X) =>
                X.Name == columName);
        }

        public IColumn GetColumnByPropertyName(string columName)
        {
            return Columns.SingleOrDefault((X) =>
                X.PropertyName == columName);
        }

        public string DropColumnSql(string columnName)
        {
            throw new NotImplementedException();
        }
        
        public DataProviders.IDataProvider Provider { get; set; }
    }
}
