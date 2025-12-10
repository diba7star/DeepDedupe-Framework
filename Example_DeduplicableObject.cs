// Example_DeduplicableObject.cs

using System;
using System.IO;
using System.Text;

/// <summary>
/// Example of a real-world object (Session) that benefits from deduplication.
/// Note how the unique ID and timestamp are deliberately excluded from GetContentBytes.
/// </summary>
public class SessionObject : IDeduplicable
{
    // Unique data that should NOT be hashed (e.g., Memory Address, Allocation ID)
    public Guid SessionId { get; set; } 
    public DateTime LastAccessTime { get; set; }

    // Core Content Data that is prone to redundancy and MUST be hashed
    public string Role { get; set; } = "guest";
    public int PermissionsMask { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// The canonical implementation of content serialization for hashing.
    /// </summary>
    public byte[] GetContentBytes()
    {
        using (var ms = new MemoryStream())
        using (var bw = new BinaryWriter(ms, Encoding.UTF8))
        {
            // 1. Write core content fields in a fixed, known order
            bw.Write(Role);
            bw.Write(PermissionsMask);
            bw.Write(IsActive);
            
            // 2. Do NOT write SessionId or LastAccessTime (they are unique)

            return ms.ToArray();
        }
    }
}

// Example usage demonstration (main function context):
public static class DeepDedupeDemo
{
    public static void RunDemo()
    {
        var deduplicator = new DeepDeduper<SessionObject>();

        // 1. Create the first object (Canonical)
        var session1 = new SessionObject { Role = "admin", PermissionsMask = 15 };
        var canonical1 = deduplicator.GetCanonicalObject(session1);

        // 2. Create an identical object (Duplicate)
        var session2 = new SessionObject { Role = "admin", PermissionsMask = 15 }; // Same content
        var canonical2 = deduplicator.GetCanonicalObject(session2);

        // 3. Create a different object (New Canonical)
        var session3 = new SessionObject { Role = "viewer", PermissionsMask = 1 };
        var canonical3 = deduplicator.GetCanonicalObject(session3);

        Console.WriteLine($"Session 1 Content Hash: {BitConverter.ToString(canonical1.GetContentBytes())}");
        Console.WriteLine($"Session 2 Content Hash: {BitConverter.ToString(canonical2.GetContentBytes())}");
        
        // Output should be TRUE: the references point to the same object
        Console.WriteLine($"Are canonical1 and canonical2 the same object in memory? {object.ReferenceEquals(canonical1, canonical2)}"); 

        // Output should be FALSE: the content is different
        Console.WriteLine($"Are canonical1 and canonical3 the same object in memory? {object.ReferenceEquals(canonical1, canonical3)}"); 

        // The key is that 'session2' was discarded, and only one "admin" object remains in the heap.
    }
}
