using Neo.SmartContract.Enumerators;
using Neo.VM;

namespace Neo.SmartContract.Iterators
{
    public interface IIterator : IEnumerator
    {
        StackItem Key();
    }
}
