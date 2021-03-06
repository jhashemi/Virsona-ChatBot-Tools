/******************************************************************\
 *      Class Name:     SalienceList
 *      Written By:     James.R
 *      Copyright:      Virsona Inc
 *
 *      Modifications:
 *      -----------------------------------------------------------
 *      Date            Author          Modification
 *
\******************************************************************/
using System;
using System.Collections.Generic;
using System.Text;
using PluggerBase.FastSerializer;

namespace DataTemple.Codeland.SearchTree
{

    // Light-weight version of SalienceTree, for small collections
    public class SalienceList<DataType> : IDictionary<double, DataType>, ISalienceSet<DataType>
    {
        protected LinkedList<double> keys;
        protected LinkedList<DataType> values;
        protected double salienceSum;

        protected DataType lastSelected;

        public Random randgen;

        public SalienceList()
        {
            keys = new LinkedList<double>();
            values = new LinkedList<DataType>();
            salienceSum = 0;
            lastSelected = default(DataType);

            randgen = new Random();
        }

        public LinkedList<double> LinkedKeys
        {
            get
            {
                return keys;
            }
        }

        public LinkedList<DataType> LinkedValues
        {
            get
            {
                return values;
            }
        }

        #region SalienceTree-Like Members

        public double SalienceTotal
        {
            get
            {
                return salienceSum;
            }
        }

        public virtual void ChangeSalience(DataType obj, double before, double after)
        {
            if (double.IsNaN(after) || double.IsInfinity(after))
                throw new ArgumentException("key must be finite.");
            LinkedListNode<double> nodekey = keys.First;
            LinkedListNode<DataType> nodevalue = values.First;
            while (nodekey != null && nodevalue != null)
            {
                if (nodevalue.Value.Equals(obj) && nodekey.Value == before)
                    break;
                nodekey = nodekey.Next;
                nodevalue = nodevalue.Next;
            }

            if (nodekey != null)    // might be null do to past floating point errors
                nodekey.Value = after;
            salienceSum += after - before;
        }

        public DataType SelectRandomItem(RandomSearchQuality quality)
        {
            if (values.First == null)
                return default(DataType);    // no elements
            if (quality == RandomSearchQuality.Fast)
            {
                LinkedListNode<DataType> first = values.First;
                if (first.Value.Equals(lastSelected))
                {
                    LinkedListNode<double> firstKey = keys.First;
                    values.RemoveFirst();
                    keys.RemoveFirst();
                    values.AddLast(first);
                    keys.AddLast(firstKey);
                    first = values.First;
                }
                lastSelected = first.Value;
                return first.Value;
            }
            else
            {
                LinkedListNode<DataType> node = values.First;
                for (int chosen = randgen.Next(values.Count); chosen > 0; chosen--)
                    node = node.Next;
                return node.Value;
            }
        }

        public DataType SelectSalientItem(RandomSearchQuality quality)
        {
            if (values.First == null)
                return default(DataType);    // no elements
            if (quality == RandomSearchQuality.Fast)
            {
                LinkedListNode<DataType> first = values.First;
                if (first.Value.Equals(lastSelected))
                {
                    // move it to the end
                    LinkedListNode<double> firstKey = keys.First;
                    values.RemoveFirst();
                    keys.RemoveFirst();
                    values.AddLast(first);
                    keys.AddLast(firstKey);
                    first = values.First;
                }
                lastSelected = first.Value;
                return first.Value;
            }
            else
            {
                double target = randgen.NextDouble() * salienceSum;
                double sofar = 0;

                LinkedListNode<double> nodekey = keys.First;
                LinkedListNode<DataType> nodevalue = values.First;
                while (nodekey != null && nodevalue != null)
                {
                    sofar += nodekey.Value;
                    if (sofar >= target)
                        return nodevalue.Value;
                    nodekey = nodekey.Next;
                    nodevalue = nodevalue.Next;
                }

                // Salience calculation failure!
                Console.WriteLine("WARNING: Salience miscalculation!  Max is " + sofar.ToString() + " not " + salienceSum.ToString());
                salienceSum = sofar;
                // Try again!
                return SelectSalientItem(quality);
            }
        }

        public virtual bool Remove(double key, DataType obj)
        {
            LinkedListNode<double> nodekey = keys.First;
            LinkedListNode<DataType> nodevalue = values.First;
            while (nodekey != null && nodevalue != null)
            {
                if (nodevalue.Value.Equals(obj) && nodekey.Value == key)
                    break;

                nodekey = nodekey.Next;
                nodevalue = nodevalue.Next;
            }

            if (nodekey != null)
            {
                salienceSum -= key;
                keys.Remove(nodekey);
                values.Remove(nodevalue);
                return true;
            }
            else
                return false;
        }

        #endregion

        #region RedBlackTree-Like Members

        public double GetMinKey()
        {
            double minkey = double.MaxValue;
            for (LinkedListNode<double> nodekey = keys.First; nodekey != null; nodekey = nodekey.Next)
                if (nodekey.Value < minkey)
                    minkey = nodekey.Value;

            return minkey;
        }

        public double GetMaxKey()
        {
            double maxkey = double.MinValue;
            for (LinkedListNode<double> nodekey = keys.First; nodekey != null; nodekey = nodekey.Next)
                if (nodekey.Value > maxkey)
                    maxkey = nodekey.Value;

            return maxkey;
        }

        #endregion

        #region IDictionary<double,DataType> Members

        public ICollection<double> Keys
        {
            get
            {
                return keys;
            }
        }

        public ICollection<DataType> Values
        {
            get
            {
                return values;
            }
        }

        public virtual void Add(double key, DataType value)
        {
            if (double.IsNaN(key) || double.IsInfinity(key))
                throw new ArgumentException("key must be finite.");
            keys.AddLast(key);
            values.AddLast(value);
            salienceSum += key;
        }

        public virtual bool Remove(double key)
        {
            LinkedListNode<double> nodekey = keys.First;
            LinkedListNode<DataType> nodevalue = values.First;
            while (nodekey != null && nodevalue != null)
            {
                if (nodekey.Value == key)
                    break;

                nodekey = nodekey.Next;
                nodevalue = nodevalue.Next;
            }

            salienceSum -= key;

            if (nodekey != null)
            {
                keys.Remove(nodekey);
                values.Remove(nodevalue);
                return true;
            }
            else
                return false;
        }

        public bool ContainsKey(double key)
        {
            for (LinkedListNode<double> nodekey = keys.First; nodekey != null; nodekey = nodekey.Next)
                if (nodekey.Value == key)
                    return true;

            return false;
        }

        public double Search(DataType obj)
        {
            LinkedListNode<double> nodekey = keys.First;
            LinkedListNode<DataType> nodevalue = values.First;
            while (nodekey != null && nodevalue != null)
            {
                if (nodevalue.Value.Equals(obj))
                    break;

                nodekey = nodekey.Next;
                nodevalue = nodevalue.Next;
            }

            if (nodekey != null)
                return nodekey.Value;
            else
                return 0;
        }

        public bool TryGetValue(double key, out DataType value)
        {
            LinkedListNode<double> nodekey = keys.First;
            LinkedListNode<DataType> nodevalue = values.First;
            while (nodekey != null && nodevalue != null)
            {
                if (nodekey.Value == key) {
                    value = nodevalue.Value;
                    return true;
                }

                nodekey = nodekey.Next;
                nodevalue = nodevalue.Next;
            }

            value = default(DataType);
            return false;
        }

        public DataType this[double key]
        {
            get
            {
                LinkedListNode<double> nodekey = keys.First;
                LinkedListNode<DataType> nodevalue = values.First;
                while (nodekey != null && nodevalue != null)
                {
                    if (nodekey.Value == key)
                        return nodevalue.Value;

                    nodekey = nodekey.Next;
                    nodevalue = nodevalue.Next;
                }

                throw new ArgumentException("Could not find " + key);
            }
            set
            {
                LinkedListNode<double> nodekey = keys.First;
                LinkedListNode<DataType> nodevalue = values.First;
                while (nodekey != null && nodevalue != null)
                {
                    if (nodekey.Value == key)
                        nodevalue.Value = value;

                    nodekey = nodekey.Next;
                    nodevalue = nodevalue.Next;
                }
            }
        }

        #endregion

        #region ICollection<KeyValuePair<double,DataType>> Members

        public int Count
        {
            get { return keys.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public void Add(KeyValuePair<double, DataType> item)
        {
            keys.AddLast(item.Key);
            values.AddLast(item.Value);
            salienceSum += item.Key;
        }

        public bool Remove(KeyValuePair<double, DataType> item)
        {
            LinkedListNode<double> nodekey = keys.First;
            LinkedListNode<DataType> nodevalue = values.First;
            while (nodekey != null && nodevalue != null)
            {
                if (nodevalue.Value.Equals(item.Value) && nodekey.Value == item.Key)
                    break;

                nodekey = nodekey.Next;
                nodevalue = nodevalue.Next;
            }

            salienceSum -= item.Key;

            if (nodekey != null)
            {
                keys.Remove(nodekey);
                values.Remove(nodevalue);
                return true;
            }
            else
                return false;
        }

        public void Clear()
        {
            keys.Clear();
            values.Clear();
            salienceSum = 0;
        }

        public bool Contains(KeyValuePair<double, DataType> item)
        {
            LinkedListNode<double> nodekey = keys.First;
            LinkedListNode<DataType> nodevalue = values.First;
            while (nodekey != null && nodevalue != null)
            {
                if (nodevalue.Value.Equals(item.Value) && nodekey.Value == item.Key)
                    return true;

                nodekey = nodekey.Next;
                nodevalue = nodevalue.Next;
            }

            return false;
        }

        public void CopyTo(KeyValuePair<double, DataType>[] array, int arrayIndex)
        {
            LinkedListNode<double> nodekey = keys.First;
            LinkedListNode<DataType> nodevalue = values.First;
            while (nodekey != null && nodevalue != null)
            {
                array[arrayIndex] = new KeyValuePair<double, DataType>(nodekey.Value, nodevalue.Value);

                nodekey = nodekey.Next;
                nodevalue = nodevalue.Next;
                arrayIndex++;
            }
        }

        #endregion

        #region IEnumerable<KeyValuePair<double,DataType>> Members

        public IEnumerator<KeyValuePair<double, DataType>> GetEnumerator()
        {
            return new SalienceListEnumerator(keys, values);
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new SalienceListEnumerator(keys, values);
        }

        #endregion

        public class SalienceListEnumerator : IEnumerator<KeyValuePair<double, DataType>>
        {
            LinkedList<double> keys;
            LinkedList<DataType> values;
            
            LinkedList<double>.Enumerator keyenum;
            LinkedList<DataType>.Enumerator valueenum;

            public SalienceListEnumerator(LinkedList<double> keys, LinkedList<DataType> values)
            {
                this.keys = keys;
                this.values = values;
                keyenum = keys.GetEnumerator();
                valueenum = values.GetEnumerator();
            }

            #region IEnumerator<KeyValuePair<double,DataType>> Members

            public KeyValuePair<double, DataType> Current
            {
                get
                {
                    return new KeyValuePair<double, DataType>(keyenum.Current, valueenum.Current);
                }
            }

            #endregion

            #region IDisposable Members

            public void Dispose()
            {
                keyenum.Dispose();
                valueenum.Dispose();
            }

            #endregion

            #region IEnumerator Members

            object System.Collections.IEnumerator.Current
            {
                get
                {
                    return new KeyValuePair<double, DataType>(keyenum.Current, valueenum.Current);
                }
            }

            public bool MoveNext()
            {
                return keyenum.MoveNext() && valueenum.MoveNext();
            }

            public void Reset()
            {
                keyenum = keys.GetEnumerator();
                valueenum = values.GetEnumerator();
            }

            #endregion
        }

        #region IFastSerializable Members

        public virtual void Deserialize(SerializationReader reader)
        {
            salienceSum = reader.ReadDouble();  // salienceSum = info.GetDouble("salienceSum");
            int count = reader.ReadInt32();
            keys = new LinkedList<double>();
            for (int ii = 0; ii < count; ii++)
                keys.AddLast(reader.ReadDouble());
            values = new LinkedList<DataType>();
            for (int ii = 0; ii < count; ii++)
                values.AddLast((DataType) reader.ReadPointer());
        }

        public virtual void Serialize(SerializationWriter writer)
        {
            writer.Write(salienceSum);  // info.AddValue("salienceSum", salienceSum);
            writer.Write(keys.Count);
            foreach (double key in keys)
                writer.Write(key);
            foreach (DataType value in values)
                writer.WritePointer(value);
        }

        #endregion
    }
}
