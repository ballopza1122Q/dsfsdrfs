// Test-only wrapper manager: exercise the pinned VM's real generic delegate
// fallback, which the supplied IFix.Core also exposes. Excluded from patch build.
using System;
using IFix.Core;
sealed class OfflineWrappers:WrappersManager
{
    public static bool FailDelegate;
    public Delegate CreateDelegate(Type type,int id,object anon){if(FailDelegate)throw new NotSupportedException("delegate unavailable");return null;}
    public AnonymousStorey CreateBridge(int fieldNum,int[] fieldTypes,int typeIndex,int[] vTable,int[] slots,VirtualMachine vm){throw new NotSupportedException();}
    public object CreateWrapper(int id){throw new NotSupportedException();}
    public object InitWrapperArray(int length){throw new NotSupportedException();}
}
