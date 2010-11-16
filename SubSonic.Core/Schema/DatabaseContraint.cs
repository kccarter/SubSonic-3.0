using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SubSonic.Schema
{
    public class DatabaseContraint
        : IConstraint
    {
        #region IConstraint Members

        public string Name { get; set; }

        public string SchemaName { get; set; }

        public string TableName { get; set; }

        public string Type { get; set; }

        #endregion

        #region constructor
        public DatabaseContraint()
        {
        }
        #endregion
    }
}
