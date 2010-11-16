using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SubSonic.Schema
{
    public interface IConstraint
    {
        string Name { get; set; }
        string SchemaName { get; set; }
        string TableName { get; set; }
        string Type { get; set; }
    }
}
