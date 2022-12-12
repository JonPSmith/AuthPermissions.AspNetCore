// Copyright (c) 2022 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using LocalizeMessagesAndErrors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TestSupport.Helpers;

namespace Test.StubClasses;

/// <summary>
/// This provides a simple replacement of the <see cref="LocalizeWithDefault{TResource}"/> which
/// returns the the default message.
/// It also writes the information on each localized message to a database is the appsettings.json
/// file in your testing project contains "SaveLocalizesToDb": true.
/// If "SaveLocalizesToDb" is True, then there needs to be a connection string called "LocalizationCaptureDb"
/// which links to a SQL Server database server where the localized message information is saved to.
/// </summary>
/// <typeparam name="TResource"></typeparam>
public class StubLocalizeDefaultWithLogging<TResource> : ILocalizeWithDefault<TResource>
{
    /// <summary>
    /// This contains a list each localization request, with extra data.
    /// Can be useful in unit tests
    /// </summary>
    public List<LocalizedLog> Logs { get; set; } = new List<LocalizedLog>();


    /// <summary>
    /// This is true if there is an entry in the database with the same Resource/Key, but a different.
    /// Used in tests of this class.
    /// </summary>
    public bool? SameKeyButDiffFormat { get; set; }



    public string LocalizeStringMessage(LocalizeKeyData localizeKeyData, string cultureOfMessage, string message)
    {
        if (localizeKeyData == null)
            throw new ArgumentNullException(nameof(localizeKeyData));

        var log = CreateLocalizedLog(localizeKeyData, cultureOfMessage, message, null);
        Logs.Add(log);

        SaveLocalizationToDb(log);
        return message;
    }

    public string LocalizeFormattedMessage(LocalizeKeyData localizeKeyData, string cultureOfMessage,
        params FormattableString[] formattableStrings)
    {
        if (localizeKeyData == null)
            throw new ArgumentNullException(nameof(localizeKeyData));

        var actualMessage = string.Join(string.Empty, formattableStrings.Select(x => x.ToString()).ToArray());
        var messageFormat = string.Join(string.Empty, formattableStrings.SelectMany(x => x.Format).ToArray());

        var log = CreateLocalizedLog(localizeKeyData, cultureOfMessage, actualMessage, messageFormat);
        Logs.Add(log);

        SaveLocalizationToDb(log);

        return string.Join(string.Empty, formattableStrings.Select(x => x.ToString()).ToArray());
    }

    //--------------------------------------------------------------
    // private methods

    private LocalizedLog CreateLocalizedLog(LocalizeKeyData localizeKeyData, string cultureOfMessage, string actualMessage,
            string? messageFormat)
    {
        var localizeKey = localizeKeyData.LocalizeKey ?? "already localize";
        var callingClassName = GetFormattedName(localizeKeyData.CallingClass);

        return new LocalizedLog(typeof(TResource), localizeKey,
            cultureOfMessage, actualMessage, messageFormat,
            callingClassName, localizeKeyData.MethodName, localizeKeyData.SourceLineNumber);
    }

    /// <summary>
    /// This adds information on each localized message, including where it was sent from,
    /// so that you can see what localized messages in your app. Usually you would use the
    /// <see cref="StubLocalizeDefaultWithLogging{TResource}"/> within your unit tests.
    /// It tries to: 
    /// 1) Add a new entry in the database if there isn't an entry containing the same information.
    /// 2) It also sets the <see cref="LocalizedLog"/>.<see cref="LocalizedLog.SameKeyButDiffFormat"/> to true
    /// if an existing entry with the same ResourceFile / LocalizeKey, but a different different message.
    /// </summary>
    /// <param name="localizedLog"></param>
    private void SaveLocalizationToDb(LocalizedLog localizedLog)
    {
        using var context = GetLocalizationCaptureDbInstance();
        if (context == null)
            return;

        //This will hold any existing database entries that have the same ResourceFile and LocalizeKey
        var sameLocalizationReference = context.LocalizedData
            .Where(x => x.ResourceClassFullName == localizedLog.ResourceClassFullName
                        && x.LocalizeKey == localizedLog.LocalizeKey).ToList();

        //This is true if there was already an entry with the same ResourceFile / LocalizeKey but a different message
        SameKeyButDiffFormat = sameLocalizationReference.Any(x =>
            x.ResourceClassFullName == typeof(TResource).FullName
            && x.LocalizeKey == localizedLog.LocalizeKey
            && ((x.MessageFormat != null && x.MessageFormat != localizedLog.MessageFormat)
                || (x.MessageFormat == null && x.ActualMessage != localizedLog.ActualMessage)));

        if (sameLocalizationReference.Any(x =>
                ((x.MessageFormat != null && x.MessageFormat == localizedLog.MessageFormat)     //Using format: and the same
                 || (x.MessageFormat == null && x.ActualMessage == localizedLog.ActualMessage))  //Using string: and the same
                && x.SameKeyButDiffFormat == SameKeyButDiffFormat
                && x.CallingClassName == localizedLog.CallingClassName
                && x.CallingMethodName == localizedLog.CallingMethodName
                && x.SourceLineNumber == localizedLog.SourceLineNumber))
            //There is already an entry with the same 
            return;

        //set the SameKey before writing to the database
        localizedLog.SameKeyButDiffFormat = SameKeyButDiffFormat;

        context.Add(localizedLog);
        context.SaveChanges();
    }

    ///thanks to https://stackoverflow.com/a/66604069/1434764
    /// <summary>
    /// Returns the type name. If this is a generic type, appends
    /// the list of generic type arguments between angle brackets.
    /// (Does not account for embedded / inner generic arguments.)
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>System.String.</returns>
    public static string GetFormattedName(Type type)
    {
        if (type.IsGenericType)
        {
            string genericArguments = type.GetGenericArguments()
                .Select(x => x.Name)
                .Aggregate((x1, x2) => $"{x1}, {x2}");
            return $"{type.Name.Substring(0, type.Name.IndexOf("`"))}"
                   + $"<{genericArguments}>";
        }
        return type.Name;
    }

    //-----------------------------------------------------------
    //database part

    public LocalizationCaptureDb? GetLocalizationCaptureDbInstance(bool turnOnManually = false)
    {
        var config = AppSettings.GetConfiguration();
        if (!turnOnManually && config["SaveLocalizesToDb"] != "True")
            return null;

        var connectionString = config.GetConnectionString("LocalizationCaptureDb");
        if (connectionString == null)
            throw new Exception("The ConnectionString: 'LocalizationCaptureDd' must be added to the appsettings file to make this work.");

        var optionsBuilder =
            new DbContextOptionsBuilder<LocalizationCaptureDb>();
        optionsBuilder.UseSqlServer(connectionString);

        var context = new LocalizationCaptureDb(optionsBuilder.Options);
        context.Database.EnsureCreated();
        return context;
    }

    public void WipeLocalizationCaptureDb()
    {
        using var context = GetLocalizationCaptureDbInstance(true);
        if (context == null)
            return;

        if (context.Database.EnsureCreated())
            return;
        //The database exists so wipe the entries
        context.RemoveRange(context.LocalizedData.ToList());
        context.SaveChanges();
    }

    public List<LocalizedLog> ListLocalizationCaptureDb()
    {
        using var context = GetLocalizationCaptureDbInstance(true);
        if (context == null)
            return null;

        return context.LocalizedData.OrderBy(l => l.ResourceClassFullName).ThenBy(l => l.LocalizeKey)
            .ToList();
    }



    public class LocalizationCaptureDb : DbContext
    {
        public LocalizationCaptureDb(DbContextOptions<LocalizationCaptureDb> options)
            : base(options) { }

        public DbSet<LocalizedLog> LocalizedData { get; set; }
    }
}