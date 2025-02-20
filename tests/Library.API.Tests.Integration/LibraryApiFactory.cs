﻿using Library.API.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Library.API.Tests.Integration;

public class LibraryApiFactory: WebApplicationFactory<IApiMarker>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(collection =>
        {
            collection.RemoveAll(typeof(IDbConnectionFactory));
            collection.AddSingleton<IDbConnectionFactory>(_ =>
                new SqliteConnectionFactory("DataSource=file:inmem?mode=memory&cache=shared"));
        });
    }
}
