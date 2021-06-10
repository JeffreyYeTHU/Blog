using System;

namespace OverrideEqualsInCSharp
{
    public sealed class Person
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    public sealed class PersonA
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }

        // Wrong! No null check
        public override bool Equals(Object obj)
        {
            var other = obj as PersonA;
            return this.FirstName == other.FirstName
                && this.LastName == other.LastName;
        }
    }

    public class PersonB
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }

        // Not the most efficient
        public override bool Equals(Object obj)
        {
            var other = obj as PersonB;
            return other is Object  // null is checked here
                && this.FirstName == other.FirstName
                && this.LastName == other.LastName;
        }
    }

    public class PersonC
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }

        // Good implementation
        public override bool Equals(Object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            var other = obj as PersonC;
            return this.FirstName.Equals(other.FirstName)
                && this.LastName.Equals(other.LastName);
        }
    }

    public sealed class PersonD
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public override bool Equals(Object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            var other = obj as PersonD;
            return this.FirstName.Equals(other.FirstName)
                && this.LastName.Equals(other.LastName);
        }

        // Wrong! Introduce a recursive call accidentally, will lead to infinite loop
        public static bool operator ==(PersonD left, PersonD right)
        {
            return left == right;
        }

        // Wrong! Introduce a recursive call accidentally, will lead to infinite loop
        public static bool operator !=(PersonD left, PersonD right)
        {
            return left != right;
        }
    }

    public class PersonE
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public override bool Equals(Object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            var other = obj as PersonE;
            return this.FirstName.Equals(other.FirstName)
                && this.LastName.Equals(other.LastName);
        }

        public static bool operator ==(PersonE left, PersonE right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        public static bool operator !=(PersonE left, PersonE right)
        {
            return !(left == right);  // Call the already implemented ==
        }
    }
}
