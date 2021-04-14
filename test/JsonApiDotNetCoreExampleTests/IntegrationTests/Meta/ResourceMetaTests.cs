using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using JsonApiDotNetCore.Configuration;
using JsonApiDotNetCore.Serialization.Objects;
using JsonApiDotNetCoreExampleTests.Startups;
using TestBuildingBlocks;
using Xunit;

namespace JsonApiDotNetCoreExampleTests.IntegrationTests.Meta
{
    public sealed class ResourceMetaTests : IClassFixture<ExampleIntegrationTestContext<TestableStartup<SupportDbContext>, SupportDbContext>>
    {
        private readonly ExampleIntegrationTestContext<TestableStartup<SupportDbContext>, SupportDbContext> _testContext;
        private readonly SupportFakers _fakers = new SupportFakers();

        public ResourceMetaTests(ExampleIntegrationTestContext<TestableStartup<SupportDbContext>, SupportDbContext> testContext)
        {
            _testContext = testContext;

            testContext.UseController<ProductFamiliesController>();
            testContext.UseController<SupportTicketsController>();

            testContext.ConfigureServicesAfterStartup(services =>
            {
                services.AddResourceDefinition<SupportTicketDefinition>();
            });
        }

        [Fact]
        public async Task Returns_resource_meta_from_ResourceDefinition()
        {
            // Arrange
            List<SupportTicket> tickets = _fakers.SupportTicket.Generate(3);
            tickets[0].Description = "Critical: " + tickets[0].Description;
            tickets[2].Description = "Critical: " + tickets[2].Description;

            await _testContext.RunOnDatabaseAsync(async dbContext =>
            {
                await dbContext.ClearTableAsync<SupportTicket>();
                dbContext.SupportTickets.AddRange(tickets);
                await dbContext.SaveChangesAsync();
            });

            const string route = "/supportTickets";

            // Act
            (HttpResponseMessage httpResponse, Document responseDocument) = await _testContext.ExecuteGetAsync<Document>(route);

            // Assert
            httpResponse.Should().HaveStatusCode(HttpStatusCode.OK);

            responseDocument.ManyData.Should().HaveCount(3);
            responseDocument.ManyData[0].Meta.Should().ContainKey("hasHighPriority");
            responseDocument.ManyData[1].Meta.Should().BeNull();
            responseDocument.ManyData[2].Meta.Should().ContainKey("hasHighPriority");
        }

        [Fact]
        public async Task Returns_resource_meta_from_ResourceDefinition_in_included_resources()
        {
            // Arrange
            ProductFamily family = _fakers.ProductFamily.Generate();
            family.Tickets = _fakers.SupportTicket.Generate(1);
            family.Tickets[0].Description = "Critical: " + family.Tickets[0].Description;

            await _testContext.RunOnDatabaseAsync(async dbContext =>
            {
                await dbContext.ClearTableAsync<ProductFamily>();
                dbContext.ProductFamilies.Add(family);
                await dbContext.SaveChangesAsync();
            });

            string route = $"/productFamilies/{family.StringId}?include=tickets";

            // Act
            (HttpResponseMessage httpResponse, Document responseDocument) = await _testContext.ExecuteGetAsync<Document>(route);

            // Assert
            httpResponse.Should().HaveStatusCode(HttpStatusCode.OK);

            responseDocument.SingleData.Should().NotBeNull();
            responseDocument.Included.Should().HaveCount(1);
            responseDocument.Included[0].Meta.Should().ContainKey("hasHighPriority");
        }
    }
}
