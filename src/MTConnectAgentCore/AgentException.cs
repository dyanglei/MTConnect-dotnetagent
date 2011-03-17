using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MTConnectAgentCore
{
    public class AgentException: Exception
    {
        public AgentException(String msg):base(msg){}
        public AgentException(String msg, Exception innerException) : base(msg, innerException) { }

        public static AgentException createUnitsDiffException(String _dataItemName, String nativeUnits, String units)
        {
            return new AgentException("nativeUnits \"" + nativeUnits + " and units \"" + units + "\" in DataItem(name = \"" + _dataItemName + "\") do not match for a conversion.");
        }

        public static AgentException createUnitsNotDefinedException(String _dataItemName, String nativeUnits)
        {
            return new AgentException("nativeUnits \"" + nativeUnits + "\" is defined, but units is not defined for DataItem(name = \"" + _dataItemName+ "\").");
        }

        public static AgentException createUnitsNotMatchWithDataItemTypeException(String _dataItemName, String _dataItemType, String units, String correntUnits)
        {
            return new AgentException("units \"" + units + "\" for DataItem(name = \"" + _dataItemName + "\" & type = \"" + _dataItemType + "\") is incorrect.  It should be \"" + correntUnits + "\".");
        }

        public static AgentException createUnitsNotMatchWithDataItemTypeAndSubTypeException(String _dataItemName, String _dataItemType, String _dataItemSubType, String units, String correntUnits)
        {
            return new AgentException("units \"" + units + "\" for DataItem(name = \"" + _dataItemName + "\", type = \"" + _dataItemType + "\", subtype = \"" + _dataItemSubType + "\") is wrong.  It should be \"" + correntUnits + "\".");
        }

        public static AgentException createUnitsConversionNotSupportedException(String _dataItemName, String nativeUnits, String units)
        {
            return new AgentException("nativeUnits \"" + nativeUnits + " to units \"" + units + "\" is not supported.  They are defined in DataItem name = \"" + _dataItemName + "\".");
        }
        
    }
 
}
