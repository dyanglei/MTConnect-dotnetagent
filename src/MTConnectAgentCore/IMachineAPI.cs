
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MTConnectAgentCore
{
    interface IMachineAPI
    {
        short StoreSample(String timestamp, String dataItemId, String value);
        short StoreEvent(String timestamp,  String dataItemId, String value, String code, String nativeCode);
        short StoreCondition(String timestamp, String dataItemId, String condition, String value, String nativeCode, String code);
    }
}
