using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Runtime.CompilerServices;

namespace SqlGenerator.Model
{
    public sealed class Column : INotifyPropertyChanged
    {
        #region FIELDS

        private string _ColumnName;
        private int _ColumnOrdinal;
        private int _ColumnSize;
        private bool _IsUnique;
        private bool _IsKey;
        private string _DataType;
        private bool _AllowDbNull;
        private bool _IsIdentity;
        private bool _IsAutoIncrement;
        private string _ProviderSpecificDataType;
        private string _DataTypeName;
        private int _NumericPrecision;
        private int _NumericScale;

        #endregion

        #region PROPERTIES

        public string ColumnName
        {
            get { return _ColumnName; }
            set
            {
                _ColumnName = value;
                OnPropertyChanged();
            }
        }

        private int ColumnOrdinal
        {
            get { return _ColumnOrdinal; }
            set
            {
                if (value == _ColumnOrdinal) return;
                _ColumnOrdinal = value;
                OnPropertyChanged();
            }
        } // 0 based position in table

        private int ColumnSize
        {
            get { return _ColumnSize; }
            set
            {
                if (value == _ColumnSize) return;
                _ColumnSize = value;
                OnPropertyChanged();
            }
        }

        private int NumericPrecision
        {
            get { return _NumericPrecision; }
            set
            {
                if (value == _NumericPrecision) return;
                _NumericPrecision = value;
                OnPropertyChanged();
            }
        }

        private int NumericScale
        {
            get { return _NumericScale; }
            set
            {
                if (value == _NumericScale) return;
                _NumericScale = value;
                OnPropertyChanged();
            }
        }

        private bool IsUnique
        {
            get { return _IsUnique; }
            set
            {
                if (value == _IsUnique) return;
                _IsUnique = value;
                OnPropertyChanged();
            }
        }

        private bool IsKey
        {
            get { return _IsKey; }
            set
            {
                if (value == _IsKey) return;
                _IsKey = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Data type for POCO generation
        /// </summary>
        private string DataType
        {
            get { return _DataType; }
            set
            {
                if (value == _DataType) return;
                _DataType = value;
                OnPropertyChanged();
            }
        }

        private bool AllowDbNull
        {
            get { return _AllowDbNull; }
            set
            {
                if (value == _AllowDbNull) return;
                _AllowDbNull = value;
                OnPropertyChanged();
            }
        }

        public bool IsIdentity
        {
            get { return _IsIdentity; }
            set
            {
                if (value == _IsIdentity) return;
                _IsIdentity = value;
                OnPropertyChanged();
            }
        }

        private bool IsAutoIncrement
        {
            get { return _IsAutoIncrement; }
            set
            {
                if (value == _IsAutoIncrement) return;
                _IsAutoIncrement = value;
                OnPropertyChanged();
            }
        }

        private string ProviderSpecificDataType
        {
            get { return _ProviderSpecificDataType; }
            set
            {
                if (value == _ProviderSpecificDataType) return;
                _ProviderSpecificDataType = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The SQL Server data type
        /// </summary>
        private string DataTypeName
        {
            get { return _DataTypeName; }
            set
            {
                if (value == _DataTypeName) return;
                _DataTypeName = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Same as DataType but with no "System." in front
        /// </summary>
        private string SmallDataType
        {
            get { return _DataType.Replace("System.", string.Empty); }
        }

        #endregion

        public Column(DataRow schemaRow)
        {
            ColumnName = schemaRow["ColumnName"].ToString();
            ColumnOrdinal = (int)schemaRow["ColumnOrdinal"];
            ColumnSize = (int)schemaRow["ColumnSize"];
            NumericPrecision = (short)schemaRow["NumericPrecision"];
            NumericScale = (short)schemaRow["NumericScale"];
            IsUnique = schemaRow["IsUnique"] != DBNull.Value && (bool)schemaRow["IsUnique"];
            IsKey = schemaRow["IsKey"] != DBNull.Value && (bool)schemaRow["IsKey"];
            DataType = schemaRow["DataType"].ToString();
            AllowDbNull = schemaRow["AllowDBNull"] != DBNull.Value && (bool)schemaRow["AllowDBNull"];
            IsIdentity = schemaRow["IsIdentity"] != DBNull.Value && (bool)schemaRow["IsIdentity"];
            IsAutoIncrement = schemaRow["IsAutoIncrement"] != DBNull.Value && (bool)schemaRow["IsAutoIncrement"];
            ProviderSpecificDataType = schemaRow["ProviderSpecificDataType"].ToString();
            DataTypeName = schemaRow["DataTypeName"].ToString();
        }

        #region METHODS

        public string SqlField
        {
            get
            {
                var sqlDataType = GetSqlDataType();

                if (AllowDbNull)
                    sqlDataType += " NULL";

                return $"\t[{ColumnName}] {sqlDataType},\n";
            }
        }

        public string SqlInsertField => $"\t\t@{ColumnName},\n";

        public string SqlUpdateField => $"\t\t[{ColumnName}] = @{ColumnName},\n";

        public string SqlParameter
        {
            get
            {
                var sqlDataType = GetSqlDataType();

                if (AllowDbNull)
                    sqlDataType += " = NULL";

                return $"\t@{ColumnName} {sqlDataType},\n";
            }
        }

        private string GetSqlDataType()
        {
            // If the column size = 2147483647, set it to a varchar(MAX), else just set it to the size
            var columnSizeString = ColumnSize.ToString() == 2147483647.ToString() ? "MAX" : ColumnSize.ToString();

            string sqlDataType = DataTypeName.ToUpper();

            if (DataTypeName.ToUpper().Contains("CHAR"))
            {
                sqlDataType = $"{DataTypeName.ToUpper()}({columnSizeString})";
            }
            else if (DataTypeName.ToUpper().Equals("DECIMAL") || DataTypeName.ToUpper().Equals("NUMERIC"))
            {
                sqlDataType = $"{DataTypeName.ToUpper()}({NumericPrecision},{NumericScale})";
            }
            return sqlDataType;
        }

        public string PocoPropertyScript()
        {
            Dictionary<string, string> pocoDataTypeDictionary = new Dictionary<string, string>
            {
                {"Boolean", "bool"},
                {"Byte", "byte"},
                {"SByte", "sbyte"},
                {"Char", "char"},
                {"Decimal", "decimal"},
                {"Double", "double"},
                {"Single", "single"},
                {"Int32", "int"},
                {"UInt32", "uint"},
                {"Int64", "long"},
                {"UInt64", "ulong"},
                {"Object", "object"},
                {"Int16", "short"},
                {"String", "string"}
            };

            var pocoDataType = pocoDataTypeDictionary[SmallDataType];

            // If the type is a value type and the db allows nulls, add the ? nullable operator at the end of the type
            var type = Type.GetType(DataType);
            if (type != null && type.IsValueType)
                pocoDataType = AllowDbNull ? pocoDataType + "?" : pocoDataType;

            return $"\tpublic {pocoDataType} {ColumnName} {{ get; set; }}{Environment.NewLine}";
        }

        #endregion

        #region INotify

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null) PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
