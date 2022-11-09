// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Linq;
using LocalizeMessagesAndErrors;

namespace Test.StubClasses;

public class StubLocalizeWithDefault<T> : ILocalizeWithDefault<T>
{

    public string LocalizeStringMessage(string localizeKey, string cultureOfMessage, string message)
    {
        return message;
    }

    public string LocalizeFormattedMessage(string localizeKey, string cultureOfMessage,
        params FormattableString[] formattableStrings)
    {
        return string.Join(string.Empty, formattableStrings.Select(x => x.ToString()).ToArray());
    }
}