using System;

namespace SubSonic.Schema
{
    public interface IColumnDefinition
    {
        int? Character_Maximum_Length { get; set; }
        string Column_Default { get; set; }
        string Column_Name { get; set; }
        string Data_Type { get; set; }
        bool IsComputed { get; set; }
        bool IsIdentity { get; set; }
        bool IsNullable { get; set; }
        int Ordinal_Position { get; set; }
        string Table_Catalog { get; set; }
        string Table_Name { get; set; }
        string Table_Schema { get; set; }
    }
}
