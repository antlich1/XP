using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SharedLib
{
    #region Interfaces

    public interface SerialObject
    {
        int ObjectID { get; set; }
        long? ParentObjectID { get; set; }
        long recordLength { get; set; }
        void ReadValueInfo(BinarySerializationStreamAnalyzer analyzer);
        T GetMemberValue<T>(int index);
    }

    public interface TypeHoldingThing
    {
        SerialObject RelevantObject { get; set; }
        BinaryTypeEnumeration? BinaryType { get; set; }
        PrimitiveTypeEnumeration? PrimitiveType { get; set; }
        ClassTypeInfo TypeInfo { get; set; }
    }

    internal interface ValueHoldingThing
    {
        object Value { get; set; }
        object ValueRefID { get; set; }
    }

    #endregion

    #region Classes

    public class BinarySerializationStreamAnalyzer
    {
        #region Fileds
        //yeah, I know, these could be better protected...
        private Dictionary<int, SerialObject> serialObjects = null;
        private Dictionary<int, BinaryLibrary> libraries = null;
        //available to the other objects, used to read from the stream
        internal BinaryReader reader = null;
        //marks the end of the serialization stream
        private bool endRecordReached = false;
        //used for returning an arbitrary number of nulls as defined by certain record types
        private int PendingNullCounter = 0;
        #endregion

        #region Properties
        public Dictionary<int, BinaryLibrary> Libraries
        {
            get { return this.libraries; }
        }
        public Dictionary<int, SerialObject> SerialObjects
        {
            get { return this.serialObjects; }
        }
        #endregion

        #region Functions
        public BinarySerializationStreamAnalyzer() { }
        public void Read(Stream inputStream)
        {
            //reset the state
            reader = new BinaryReader(inputStream, Encoding.UTF8);
            endRecordReached = false;
            serialObjects = new Dictionary<int, SerialObject>();
            libraries = new Dictionary<int, BinaryLibrary>();

            //dig in
            while (!endRecordReached)
            {
                ParseRecord(null);
            }
            inputStream.Close();
        }
        public void Read(byte[] dataGraph)
        {
            MemoryStream stream = new MemoryStream();
            stream.Write(dataGraph, 0, dataGraph.Length);
            stream.Seek(0, SeekOrigin.Begin);
            this.Read(stream);
        }
        public string Analyze()
        {
            int classCount = 0;
            int arrayCount = 0;
            int stringCount = 0;
            long classLength = 0;
            long arrayLength = 0;
            long stringLength = 0;
            Dictionary<string, int> ObjectCounts = new Dictionary<string, int>();
            Dictionary<string, long> ObjectLengths = new Dictionary<string, long>();

            //we are only interested in top-level objects, not nested ones (otherwise would double-count lengths!).
            foreach (SerialObject someObject in serialObjects.Values)
            {
                if (someObject.ParentObjectID == null)
                {
                    if (someObject.GetType() == typeof(ClassInfo))
                    {
                        classCount++;
                        classLength += someObject.recordLength;

                        ClassInfo interestingClass = (ClassInfo)someObject;
                        if (interestingClass.ReferencedObject != null)
                            interestingClass = (ClassInfo)serialObjects[interestingClass.ReferencedObject.Value];

                        if (!ObjectCounts.ContainsKey(interestingClass.Name))
                        {
                            ObjectCounts.Add(interestingClass.Name, 0);
                            ObjectLengths.Add(interestingClass.Name, 0);
                        }

                        ObjectCounts[interestingClass.Name]++;
                        ObjectLengths[interestingClass.Name] += someObject.recordLength;
                    }
                    else if (someObject.GetType() == typeof(BinaryArray))
                    {
                        arrayCount++;
                        arrayLength += someObject.recordLength;
                    }
                    else if (someObject.GetType() == typeof(ObjectString))
                    {
                        stringCount++;
                        stringLength += someObject.recordLength;
                    }
                }
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Format("Total Objects: {0}", serialObjects.Count));
            sb.AppendLine(string.Format("Total Top-Level Objects: {0}", classCount + arrayCount + stringCount));
            sb.AppendLine(string.Format("Total Top-Level Length: {0}", classLength + arrayLength + stringLength));
            sb.AppendLine();
            sb.AppendLine(string.Format("Top-Level Class Count: {0}", classCount));
            sb.AppendLine(string.Format("Top-Level Class Length: {0}", classLength));
            sb.AppendLine();
            sb.AppendLine(string.Format("Top-Level Array Count: {0}", arrayCount));
            sb.AppendLine(string.Format("Top-Level Array Length: {0}", arrayLength));
            sb.AppendLine();
            sb.AppendLine(string.Format("Top-Level String Count: {0}", stringCount));
            sb.AppendLine(string.Format("Top-Level String Length: {0}", stringLength));
            sb.AppendLine();
            sb.AppendLine("Top-Level Object Counts by Name:");
            foreach (string ClassName in ObjectCounts.Keys)
            {
                sb.AppendLine(string.Format("{0}: {1}", ClassName, ObjectCounts[ClassName]));
            }
            sb.AppendLine();
            sb.AppendLine("Top-Level Object Lengths by Name:");
            foreach (string ClassName in ObjectLengths.Keys)
            {
                sb.AppendLine(string.Format("{0}: {1}", ClassName, ObjectLengths[ClassName]));
            }

            return sb.ToString();
        }
        internal int? ParseRecord(SerialObject parentObject)
        {
            int? serialObjectReferenceID = null;
            if (PendingNullCounter == 0)
            {
                long startPosition = reader.BaseStream.Position;
                SerialObject si = null;
                RecordTypeEnumeration nextRecordType = (RecordTypeEnumeration)reader.ReadByte();
                switch (nextRecordType)
                {
                    case RecordTypeEnumeration.SerializedStreamHeader:
                        //header is 4 values that I wouldn't know what to do with (what type of message, what version, etc) - trash.
                        reader.ReadBytes(16);
                        break;
                    case RecordTypeEnumeration.ClassWithID:
                        //just two ints, read directly
                        si = new ClassInfo();
                        si.ObjectID = reader.ReadInt32();
                        ((ClassInfo)si).ReferencedObject = reader.ReadInt32();
                        //Use the referenced object definition for data retrieval rules
                        // -> this will overwrite the original values in the referenced object, but who cares - the values are trash anyway (for now).
                        ((ClassInfo)serialObjects[((ClassInfo)si).ReferencedObject.Value]).ReadValueInfo(this);                     
                        break;
                    case RecordTypeEnumeration.SystemClassWithMembers:
                        //single structure, read in constructor
                        si = new ClassInfo(this);
                        //also values.
                        si.ReadValueInfo(this);
                        break;
                    case RecordTypeEnumeration.ClassWithMembers:
                        //single structure, read in constructor
                        si = new ClassInfo(this);
                        //also library ID, read into place.
                        ((ClassInfo)si).LibraryID = reader.ReadInt32();
                        //also values.
                        si.ReadValueInfo(this);
                        break;
                    case RecordTypeEnumeration.SystemClassWithMembersAndTypes:
                        //single structure, read in constructor
                        si = new ClassInfo(this);
                        //also member type info, read into place.
                        ((ClassInfo)si).ReadTypeInfo(this);
                        //also values.
                        si.ReadValueInfo(this);
                        break;
                    case RecordTypeEnumeration.ClassWithMembersAndTypes:
                        //single structure, read in constructor
                        si = new ClassInfo(this);
                        //also member type info, read into place.
                        ((ClassInfo)si).ReadTypeInfo(this);
                        //also library ID, read into place.
                        ((ClassInfo)si).LibraryID = reader.ReadInt32();
                        //also values.
                        si.ReadValueInfo(this);
                        break;
                    case RecordTypeEnumeration.BinaryObjectString:
                        //simple structure, just an ID and a string
                        si = new ObjectString();
                        si.ObjectID = reader.ReadInt32();
                        ((ObjectString)si).String = reader.ReadString();
                        break;
                    case RecordTypeEnumeration.BinaryArray:
                        //complex process, read in constructor.
                        si = new BinaryArray(this);
                        //also values.
                        si.ReadValueInfo(this);
                        break;
                    case RecordTypeEnumeration.MemberReference:
                        //just return the ID that was referenced.
                        serialObjectReferenceID = reader.ReadInt32();
                        break;
                    case RecordTypeEnumeration.ObjectNull:
                        //a single null; do nothing, as null is the default return value.
                        break;
                    case RecordTypeEnumeration.MessageEnd:
                        //do nothing, quit. Wasn't that fun?
                        endRecordReached = true;
                        break;
                    case RecordTypeEnumeration.BinaryLibrary:
                        int newLibraryID = reader.ReadInt32();
                        libraries.Add(newLibraryID, new BinaryLibrary());
                        libraries[newLibraryID].LibraryID = newLibraryID;
                        libraries[newLibraryID].Name = reader.ReadString();
                        libraries[newLibraryID].recordLength = reader.BaseStream.Position - startPosition;
                        break;
                    case RecordTypeEnumeration.ObjectNullMultiple256:
                        //a sequence of nulls; return null, and start a counter to continue returning N nulls over the next calls.
                        PendingNullCounter = reader.ReadByte() - 1;
                        break;

                    case RecordTypeEnumeration.ObjectNullMultiple:
                        //a sequence of nulls; return null, and start a counter to continue returning N nulls over the next calls.
                        PendingNullCounter = reader.ReadInt32() - 1;
#if (DEBUG)
                        //not yet tested: if it happens, take a look around.
                        System.Diagnostics.Debugger.Break();
#endif
                        break;
                    case RecordTypeEnumeration.ArraySinglePrimitive:
                        //This one's pretty easy to build, do locally.
                        si = new BinaryArray();
                        si.ObjectID = reader.ReadInt32();
                        ((BinaryArray)si).ArrayType = BinaryArrayTypeEnumeration.Single;
                        ((BinaryArray)si).BinaryType = BinaryTypeEnumeration.Primitive;
                        ((BinaryArray)si).Rank = 1;
                        ((BinaryArray)si).Lengths = new List<int>();
                        ((BinaryArray)si).Lengths.Add(reader.ReadInt32());
                        ((BinaryArray)si).PrimitiveType = (PrimitiveTypeEnumeration)reader.ReadByte();
                        //and then read the values.
                        si.ReadValueInfo(this);
                        break;
                    case RecordTypeEnumeration.ArraySingleObject:
                        //This should be pretty easy to build, do locally.
                        si = new BinaryArray();
                        si.ObjectID = reader.ReadInt32();
                        ((BinaryArray)si).ArrayType = BinaryArrayTypeEnumeration.Single;
                        ((BinaryArray)si).BinaryType = BinaryTypeEnumeration.Object;
                        ((BinaryArray)si).Rank = 1;
                        ((BinaryArray)si).Lengths = new List<int>();
                        ((BinaryArray)si).Lengths.Add(reader.ReadInt32());
                        //and then read the values.
                        si.ReadValueInfo(this);
#if (DEBUG)
                        //not yet tested: if it happens, take a look around.
                        System.Diagnostics.Debugger.Break();
#endif
                        break;
                    case RecordTypeEnumeration.ArraySingleString:
                        //This should be pretty easy to build, do locally.
                        si = new BinaryArray();
                        si.ObjectID = reader.ReadInt32();
                        ((BinaryArray)si).ArrayType = BinaryArrayTypeEnumeration.Single;
                        ((BinaryArray)si).BinaryType = BinaryTypeEnumeration.String;
                        ((BinaryArray)si).Rank = 1;
                        ((BinaryArray)si).Lengths = new List<int>();
                        ((BinaryArray)si).Lengths.Add(reader.ReadInt32());
                        //and then read the values.
                        si.ReadValueInfo(this);
#if (DEBUG)
                        //not yet tested: if it happens, take a look around.
                        System.Diagnostics.Debugger.Break();
#endif
                        break;
                    case RecordTypeEnumeration.MemberPrimitiveTyped:
#if (DEBUG)
                        //not yet tested: if it happens, take a look around.
                        System.Diagnostics.Debugger.Break();
#endif
                        break;
                    case RecordTypeEnumeration.MethodCall:
#if (DEBUG)
                        //not yet tested: if it happens, take a look around.
                        System.Diagnostics.Debugger.Break();
#endif
                        break;
                    case RecordTypeEnumeration.MethodReturn:
#if (DEBUG)
                        //not yet tested: if it happens, take a look around.
                        System.Diagnostics.Debugger.Break();
#endif
                        break;
                    default:
                        throw new Exception("Parsing appears to have failed dramatically. Unknown record type, we must be lost in the bytestream!");

                }

                //standard: if this was a serial object, add to list and record its length.
                if (si != null)
                {
                    serialObjects.Add(si.ObjectID, si);
                    serialObjects[si.ObjectID].recordLength = reader.BaseStream.Position - startPosition;
                    if (parentObject != null) serialObjects[si.ObjectID].ParentObjectID = parentObject.ObjectID;
                    return si.ObjectID;
                }
            }
            else
                PendingNullCounter--;
            return serialObjectReferenceID;
        }
        #endregion
    }

    public class BinaryLibrary
    {
        public int LibraryID;
        public string Name;
        public long recordLength;
    }

    public class ClassInfo : SerialObject
    {
        internal ClassInfo() { }

        internal ClassInfo(BinarySerializationStreamAnalyzer analyzer)
        {
            ObjectID = analyzer.reader.ReadInt32();
            Name = analyzer.reader.ReadString();
            Members = new List<MemberInfo>(analyzer.reader.ReadInt32());
            for (int i = 0; i < Members.Capacity; i++)
            {
                Members.Add(new MemberInfo());
                Members[i].Name = analyzer.reader.ReadString();
                Members[i].RelevantObject = this;
            }
        }

        internal void ReadTypeInfo(BinarySerializationStreamAnalyzer analyzer)
        {
            //first get binary types
            foreach (MemberInfo member in Members)
            {
                member.BinaryType = (BinaryTypeEnumeration)analyzer.reader.ReadByte();
            }

            //then get additional infos where appropriate
            foreach (MemberInfo member in Members)
            {
                TypeHelper.GetTypeAdditionalInfo(member, analyzer);
            }
        }

        public void ReadValueInfo(BinarySerializationStreamAnalyzer analyzer)
        {
            //then get additional infos where appropriate
            foreach (MemberInfo member in Members)
            {
                TypeHelper.GetTypeValue(member, member, analyzer);
            }
        }

        public int ObjectID { get; set; }
        public long? ParentObjectID { get; set; }
        public int? LibraryID;
        public int? ReferencedObject;
        public string Name;
        public List<MemberInfo> Members;
        public int ReferenceCount;

        public long recordLength { get; set; }
        public T GetMemberValue<T>(int index)
        {
            MemberInfo val = this.Members[index];
            return (T)val.Value;
        }
    }

    public class MemberInfo : TypeHoldingThing, ValueHoldingThing
    {
        public string Name;
        public SerialObject RelevantObject { get; set; }
        public BinaryTypeEnumeration? BinaryType { get; set; }
        public PrimitiveTypeEnumeration? PrimitiveType { get; set; }
        public ClassTypeInfo TypeInfo { get; set; }
        public object Value { get; set; }
        public object ValueRefID { get; set; }
    }

    public class ClassTypeInfo
    {
        public string TypeName;
        public int? LibraryID;
    }

    public class ObjectString : SerialObject
    {
        public void ReadValueInfo(BinarySerializationStreamAnalyzer analyzer)
        {
            throw new NotImplementedException();
        }

        public int ObjectID { get; set; }
        public long? ParentObjectID { get; set; }
        public string String;
        public long recordLength { get; set; }
        public T GetMemberValue<T>(int index)
        {
            return default(T);
        }
    }

    public class BinaryArray : SerialObject, TypeHoldingThing
    {
        internal BinaryArray() { }

        internal BinaryArray(BinarySerializationStreamAnalyzer analyzer)
        {
            ObjectID = analyzer.reader.ReadInt32();
            ArrayType = (BinaryArrayTypeEnumeration)analyzer.reader.ReadByte();
            Rank = analyzer.reader.ReadInt32();

            Lengths = new List<int>(Rank);
            for (int i = 0; i < Rank; i++)
            {
                Lengths.Add(analyzer.reader.ReadInt32());
            }

            if (ArrayType == BinaryArrayTypeEnumeration.SingleOffset ||
                    ArrayType == BinaryArrayTypeEnumeration.JaggedOffset ||
                    ArrayType == BinaryArrayTypeEnumeration.RectangularOffset)
            {
                LowerBounds = new List<int>(Rank);
                for (int i = 0; i < Rank; i++)
                    LowerBounds.Add(analyzer.reader.ReadInt32());
            }

            BinaryType = (BinaryTypeEnumeration)analyzer.reader.ReadByte();
            TypeHelper.GetTypeAdditionalInfo(this, analyzer);
        }

        public void ReadValueInfo(BinarySerializationStreamAnalyzer analyzer)
        {
            MemberInfo junk = new MemberInfo();
            for (int i = 0; i < Slots; i++)
                TypeHelper.GetTypeValue(this, junk, analyzer);
        }

        public int ObjectID { get; set; }
        public long? ParentObjectID { get; set; }
        public SerialObject RelevantObject { get { return this; } set { throw new NotImplementedException(); } }
        public BinaryArrayTypeEnumeration ArrayType;
        public int Rank;
        public List<int> Lengths;
        public List<int> LowerBounds;
        public BinaryTypeEnumeration? BinaryType { get; set; }
        public PrimitiveTypeEnumeration? PrimitiveType { get; set; }
        public ClassTypeInfo TypeInfo { get; set; }

        private int Slots
        {
            get
            {
                int outValue = 1;
                foreach (int length in Lengths)
                    outValue = outValue * length;
                return outValue;
            }
        }

        public long recordLength { get; set; }
        public T GetMemberValue<T>(int index)
        {
            return default(T);
        }

    }

    internal static class TypeHelper
    {
        internal static void GetTypeAdditionalInfo(TypeHoldingThing typeHolder, BinarySerializationStreamAnalyzer analyzer)
        {
            switch (typeHolder.BinaryType)
            {
                case BinaryTypeEnumeration.Primitive:
                    typeHolder.PrimitiveType = (PrimitiveTypeEnumeration)analyzer.reader.ReadByte();
                    break;
                case BinaryTypeEnumeration.String:
                    break;
                case BinaryTypeEnumeration.Object:
                    break;
                case BinaryTypeEnumeration.SystemClass:
                    typeHolder.TypeInfo = new ClassTypeInfo();
                    typeHolder.TypeInfo.TypeName = analyzer.reader.ReadString();
                    break;
                case BinaryTypeEnumeration.Class:
                    typeHolder.TypeInfo = new ClassTypeInfo();
                    typeHolder.TypeInfo.TypeName = analyzer.reader.ReadString();
                    typeHolder.TypeInfo.LibraryID = analyzer.reader.ReadInt32();
                    break;
                case BinaryTypeEnumeration.ObjectArray:
                    break;
                case BinaryTypeEnumeration.StringArray:
                    break;
                case BinaryTypeEnumeration.PrimitiveArray:
                    typeHolder.PrimitiveType = (PrimitiveTypeEnumeration)analyzer.reader.ReadByte();
                    break;
            }
        }

        internal static void GetTypeValue(TypeHoldingThing typeHolder, ValueHoldingThing valueHolder, BinarySerializationStreamAnalyzer analyzer)
        {
            switch (typeHolder.BinaryType)
            {
                case BinaryTypeEnumeration.Primitive:
                    switch (typeHolder.PrimitiveType)
                    {
                        case PrimitiveTypeEnumeration.Boolean:
                            valueHolder.Value = analyzer.reader.ReadBoolean();
                            break;
                        case PrimitiveTypeEnumeration.Byte:
                            valueHolder.Value = analyzer.reader.ReadByte();
                            break;
                        case PrimitiveTypeEnumeration.Char:
                            valueHolder.Value = analyzer.reader.ReadChar();
                            break;
                        case PrimitiveTypeEnumeration.DateTime:
                            valueHolder.Value = DateTime.FromBinary(analyzer.reader.ReadInt64());
                            break;
                        case PrimitiveTypeEnumeration.Decimal:
                            valueHolder.Value = analyzer.reader.ReadDecimal();
                            break;
                        case PrimitiveTypeEnumeration.Double:
                            valueHolder.Value = analyzer.reader.ReadDouble();
                            break;
                        case PrimitiveTypeEnumeration.Int16:
                            valueHolder.Value = analyzer.reader.ReadInt16();
                            break;
                        case PrimitiveTypeEnumeration.Int32:
                            valueHolder.Value = analyzer.reader.ReadInt32();
                            break;
                        case PrimitiveTypeEnumeration.Int64:
                            valueHolder.Value = analyzer.reader.ReadInt64();
                            break;
                        case PrimitiveTypeEnumeration.Null:
                            valueHolder.Value = null;
                            break;
                        case PrimitiveTypeEnumeration.SByte:
                            valueHolder.Value = analyzer.reader.ReadSByte();
                            break;
                        case PrimitiveTypeEnumeration.Single:
                            valueHolder.Value = analyzer.reader.ReadSingle();
                            break;
                        case PrimitiveTypeEnumeration.String:
                            valueHolder.Value = analyzer.reader.ReadString();
                            break;
                        case PrimitiveTypeEnumeration.TimeSpan:
                            valueHolder.Value = TimeSpan.FromTicks(analyzer.reader.ReadInt64());
                            break;
                        case PrimitiveTypeEnumeration.UInt16:
                            valueHolder.Value = analyzer.reader.ReadUInt16();
                            break;
                        case PrimitiveTypeEnumeration.UInt32:
                            valueHolder.Value = analyzer.reader.ReadUInt32();
                            break;
                        case PrimitiveTypeEnumeration.UInt64:
                            valueHolder.Value = analyzer.reader.ReadUInt64();
                            break;
                    }
                    break;
                case BinaryTypeEnumeration.String:
                    valueHolder.ValueRefID = analyzer.ParseRecord(typeHolder.RelevantObject);
                    break;
                case BinaryTypeEnumeration.Object:
                    valueHolder.ValueRefID = analyzer.ParseRecord(typeHolder.RelevantObject);
                    break;
                case BinaryTypeEnumeration.SystemClass:
                    valueHolder.ValueRefID = analyzer.ParseRecord(typeHolder.RelevantObject);
                    break;
                case BinaryTypeEnumeration.Class:
                    valueHolder.ValueRefID = analyzer.ParseRecord(typeHolder.RelevantObject);
                    break;
                case BinaryTypeEnumeration.ObjectArray:
                    valueHolder.ValueRefID = analyzer.ParseRecord(typeHolder.RelevantObject);
                    break;
                case BinaryTypeEnumeration.StringArray:
                    valueHolder.ValueRefID = analyzer.ParseRecord(typeHolder.RelevantObject);
                    break;
                case BinaryTypeEnumeration.PrimitiveArray:
                    valueHolder.ValueRefID = analyzer.ParseRecord(typeHolder.RelevantObject);
                    break;
            }
        }
    }

    #endregion

    #region Enumerations

    public enum RecordTypeEnumeration : int
    {
        SerializedStreamHeader = 0,
        ClassWithID = 1,                    //Object,
        SystemClassWithMembers = 2,         //ObjectWithMap,
        ClassWithMembers = 3,               //ObjectWithMapAssemId,
        SystemClassWithMembersAndTypes = 4, //ObjectWithMapTyped,
        ClassWithMembersAndTypes = 5,       //ObjectWithMapTypedAssemId,
        BinaryObjectString = 6,             //ObjectString,
        BinaryArray = 7,                    //Array,
        MemberPrimitiveTyped = 8,
        MemberReference = 9,
        ObjectNull = 10,
        MessageEnd = 11,
        BinaryLibrary = 12,                 //Assembly,
        ObjectNullMultiple256 = 13,
        ObjectNullMultiple = 14,
        ArraySinglePrimitive = 15,
        ArraySingleObject = 16,
        ArraySingleString = 17,
        //CrossAppDomainMap,
        //CrossAppDomainString,
        //CrossAppDomainAssembly,
        MethodCall = 21,
        MethodReturn = 22
    }

    public enum BinaryTypeEnumeration : int
    {
        Primitive = 0,
        String = 1,
        Object = 2,
        SystemClass = 3,
        Class = 4,
        ObjectArray = 5,
        StringArray = 6,
        PrimitiveArray = 7
    }

    public enum PrimitiveTypeEnumeration : int
    {
        Boolean = 1,
        Byte = 2,
        Char = 3,
        //unused
        Decimal = 5,
        Double = 6,
        Int16 = 7,
        Int32 = 8,
        Int64 = 9,
        SByte = 10,
        Single = 11,
        TimeSpan = 12,
        DateTime = 13,
        UInt16 = 14,
        UInt32 = 15,
        UInt64 = 16,
        Null = 17,
        String = 18
    }

    public enum BinaryArrayTypeEnumeration : int
    {
        Single = 0,
        Jagged = 1,
        Rectangular = 2,
        SingleOffset = 3,
        JaggedOffset = 4,
        RectangularOffset = 5
    }

    #endregion
}
