// DeepDedupe_Core.cs

using System.Collections.Concurrent;
using System.Security.Cryptography; 

/// <summary>
/// A conceptual implementation of a Content-Addressable Object Deduplicator.
/// Constraints T to be an IDeduplicable object to enforce deterministic content serialization.
/// </summary>
public class DeepDeduper<T> where T : class, IDeduplicable // اعمال محدودیت
{
    // Key: Content Hash (string representation of SHA-256)
    // Value: The actual canonical, shared object reference
    private readonly ConcurrentDictionary<string, T> _canonicalObjects = new ConcurrentDictionary<string, T>();

    /// <summary>
    /// Gets a shared reference for the given object content, creating a canonical 
    /// object if it does not already exist.
    /// </summary>
    public T GetCanonicalObject(T newObject)
    {
        // --- 1. Serialize and Hash ---
        // Calling the contractual method GetContentBytes()
        byte[] objectBytes = newObject.GetContentBytes(); 
        string contentHash = ComputeSHA256Hash(objectBytes);

        // --- 2. Deduplication Check (The Magic) ---
        if (_canonicalObjects.TryGetValue(contentHash, out T canonicalObject))
        {
            // Collision found! Return the existing, shared object.
            return canonicalObject;
        }
        else
        {
            // First time seeing this content. Store the new object as canonical.
            _canonicalObjects.TryAdd(contentHash, newObject);
            return newObject;
        }
    }

    private string ComputeSHA256Hash(byte[] bytes)
    {
        // Using a cryptographic hash function ensures minimal collision probability
        using (var sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(bytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }
    }
}
