using Neo.VM;
using System;

namespace Neo.SmartContract.Enumerators
{
    public interface IEnumerator : IDisposable
    {
        bool Next();
        StackItem Value();
    }
}
