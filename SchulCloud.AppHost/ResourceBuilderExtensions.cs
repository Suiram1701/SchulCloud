﻿using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchulCloud.AppHost;

internal static class ResourceBuilderExtensions
{
    /// <summary>
    /// Adds the default health check endpoint offered by ServiceDefaults to the resource.
    /// </summary>
    /// <remarks>
    /// The endpoints are only added when the application is is development environment.
    /// </remarks>
    /// <param name="builder">The resource builder.</param>
    /// <returns>The resource builder pipeline.</returns>
    public static IResourceBuilder<ProjectResource> WithDefaultHealthChecks(this IResourceBuilder<ProjectResource> builder)
    {
        if (builder.ApplicationBuilder.Environment.IsDevelopment())
        {
            builder
                .WithHttpHealthCheck(path: "/alive")
                .WithHttpHealthCheck(path: "/health");
        }

        return builder;
    }
}