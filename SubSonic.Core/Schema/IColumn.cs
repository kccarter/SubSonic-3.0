﻿using System.Data;

namespace SubSonic.Schema
{
    public interface IColumn : IDBObject
    {
        bool IsForeignKey { get; set; }
        ITable Table { get; set; }
        DbType DataType { get; set; }
        int MaxLength { get; set; }
        bool IsNullable { get; set; }
        bool IsReadOnly { get; set; }
        bool IsComputed { get; set; }
        bool IsUnique { get; set; }
        bool AutoIncrement { get; set; }
        int NumberScale { get; set; }
        int NumericPrecision { get; set; }
        bool IsPrimaryKey { get; set; }
        object DefaultSetting { get; set; }
        object DefaultValue { get; }
        string ParameterName { get; }
        string PropertyName { get; set; }
        /// <summary>
        /// Get or Set the sql statement for a computed column
        /// </summary>
        string ComputedSQL { get; set; }

        ITable ForeignKeyTo { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is numeric.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is numeric; otherwise, <c>false</c>.
        /// </value>
        bool IsNumeric { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is date time.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is date time; otherwise, <c>false</c>.
        /// </value>
        bool IsDateTime { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is string.
        /// </summary>
        /// <value><c>true</c> if this instance is string; otherwise, <c>false</c>.</value>
        bool IsString { get; }
        /// <summary>
        /// Gets a value indicating whether this instance is binary
        /// </summary>
        /// <value><c>true</c> if this instance is binary; otherwise, <c>false</c>.</value>
        bool IsBinary { get; }
         /// <summary>
        /// Gets a value indicating whether this instance is boolean.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is boolean; otherwise, <c>false</c>.
        /// </value>
        bool IsBoolean { get; }
        string CreateSql { get; }
        string AlterSql { get; }
        string DeleteSql { get; }
        string ConstraintSql { get; }

        bool HasDefaultConstraint { get; }
    }
}