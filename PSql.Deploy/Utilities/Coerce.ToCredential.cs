// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Collections;
using System.Net;
using System.Security;

namespace PSql.Deploy;

partial class Coerce // -> Credential
{
    private static NetworkCredential? ToCredential(this object? obj)
    {
        if (obj is null)
            return null;

        return obj is PSObject pSObject
            ? ToCredentialSpecialCase(pSObject.BaseObject) ?? ToCredentialFromPSObject(pSObject)
            : ToCredentialSpecialCase(obj)                 ?? ToCredentialFromOther(obj);
    }

    private static NetworkCredential? ToCredentialSpecialCase(object obj)
    {
        if (obj is NetworkCredential credential)
            return credential;

        if (obj is PSCredential psCredential && psCredential != PSCredential.Empty)
            return psCredential.GetNetworkCredential();

        if (obj is IDictionary dictionary)
            return ToCredentialFromDictionary(dictionary);

        return null;
    }

    private static NetworkCredential? ToCredentialFromDictionary(IDictionary dictionary)
    {
        return ToCredentialFromStructure(dictionary, DictionaryAccessor);
    }

    private static NetworkCredential? ToCredentialFromPSObject(PSObject pSObject)
    {
        return ToCredentialFromStructure(pSObject, PSObjectAccessor);
    }

    private static NetworkCredential? ToCredentialFromOther(object obj)
    {
        return ToCredentialFromStructure(WithType(obj), ObjectAccessor);
    }

    private static NetworkCredential? ToCredentialFromStructure<T>(
        T                        source,
        Func<T, string, object?> accessor)
    {
        var username
            =  accessor(source, "UserId")  .ToNonEmptyString()
            ?? accessor(source, "Username").ToNonEmptyString()
            ?? accessor(source, "UserName").ToNonEmptyString();
        if (username is null)
            return null;

        var password = accessor(source, "Password");

        if (password is SecureString securePassword)
            return new(username, securePassword);

        if (password.ToNonEmptyString() is { } textPassword)
            return new(username, textPassword);

        return null;
    }
}
