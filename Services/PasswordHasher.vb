Imports System.Security.Cryptography

Namespace Services
    ' PBKDF2 (SHA256, 100k iterations) with a random per-password salt.
    ' Stored as "{iterations}.{saltBase64}.{hashBase64}" so the iteration count
    ' can be raised later without invalidating already-hashed passwords.
    Public Module PasswordHasher
        Private Const SaltSize As Integer = 16
        Private Const HashSize As Integer = 32
        Private Const Iterations As Integer = 100000

        Public Function Hash(password As String) As String
            Dim salt = RandomNumberGenerator.GetBytes(SaltSize)
            Dim derivedHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize)
            Return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(derivedHash)}"
        End Function

        Public Function Verify(password As String, stored As String) As Boolean
            Dim parts = stored.Split("."c)
            If parts.Length <> 3 Then Return False

            Dim iterations = Integer.Parse(parts(0))
            Dim salt = Convert.FromBase64String(parts(1))
            Dim expectedHash = Convert.FromBase64String(parts(2))

            Dim actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expectedHash.Length)
            Return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash)
        End Function
    End Module
End Namespace
