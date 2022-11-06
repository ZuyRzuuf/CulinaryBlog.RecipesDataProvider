using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using MySql.Data.MySqlClient;
using RecipesDataProvider.Domain.Dto;
using RecipesDataProvider.Domain.Entities;
using RecipesDataProvider.Infrastructure.Exceptions;
using RecipesDataProvider.Infrastructure.Repositories;
using RecipesDataProvider.Infrastructure.Test.Integration.Database.TestData;
using RecipesDataProvider.Infrastructure.Test.Integration.Fixtures;
using Xunit;

namespace RecipesDataProvider.Infrastructure.Test.Integration.Repositories;

public class RecipeRepositoryTest : IClassFixture<RecipeRepositoryFixture>
{
    private readonly RecipeRepositoryFixture _fixture;
    
    public RecipeRepositoryTest(RecipeRepositoryFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetRecipes_ReturnsRecipesList_WhenNoExceptionIsThrown()
    {
        var sut = _fixture.Sut;
        const int expectedItemsNumber = RecipesDataCollection.ItemsNumber;

        var expected = _fixture.RecipesCollection;
        var result = await sut.GetRecipes();

        result.Should()
            .NotBeEmpty()
            .And
            .BeOfType<List<Recipe>>()
            .And
            .HaveCount(expectedItemsNumber)
            .And
            .OnlyHaveUniqueItems()
            .And
            .NotContainNulls(r => r.Title);
    }

    [Fact]
    public async Task GetRecipes_ThrowsDatabaseConnectionProblemException_WhenDatabaseReturnsException()
    {
        var sut = new RecipeRepository(_fixture.MysqlTestContextWithoutSchema, _fixture.Logger);

        await sut.Invoking(r => r
            .GetRecipes()).Should().ThrowAsync<UnknownDatabaseException>();
    }

    [Fact]
    public async Task CreateRecipe_ReturnsRecipe_WhenRecipeIsCreated()
    {
        const int numberRecipesInDatabase = RecipesDataCollection.ItemsNumber;

        var recipeDto = new CreateRecipeDto
        {
            Title = new Bogus.DataSets.Lorem().Sentence(2)
        };
        var sut = _fixture.Sut;
        var createdRecipe = await sut.CreateRecipe(recipeDto);
        var databaseContent = await sut.GetRecipes();

        using (new AssertionScope())
        {
            createdRecipe.Should()
                .NotBeNull()
                .And
                .BeOfType<Recipe>();
            createdRecipe.Title.Should().Be(recipeDto.Title);
            databaseContent.Should()
                .HaveCount(numberRecipesInDatabase + 1)
                .And
                .ContainItemsAssignableTo<Recipe>()
                .And
                .ContainEquivalentOf(createdRecipe);
        }
    }

    [Fact]
    public async Task CreateRecipe_ThrowsRecipeHasToBeUniqueException_WhenRecipeTitleExists()
    {
        var sut = _fixture.Sut;
        var existingDatabaseContent = await sut.GetRecipes();
        var existingRecipe = existingDatabaseContent.FirstOrDefault();
        var recipeDto = new CreateRecipeDto
        {
            Title = existingRecipe!.Title
        };
        
        await sut.Invoking(r => r
            .CreateRecipe(recipeDto)).Should().ThrowAsync<RecipeHasToBeUniqueException>();
    }

    [Fact]
    public async Task UpdateRecipe_Returns1_WhenRecipeIsUpdated()
    {
        var sut = _fixture.Sut;
        var baseRecipesList = await sut.GetRecipes();
        var recipeToUpdate = baseRecipesList.First();
        var recipeDto = new UpdateRecipeDto
        {
            Uuid = recipeToUpdate.Uuid,
            Title = new Bogus.DataSets.Lorem().Sentence(2)
        };

        var response = await sut.UpdateRecipe(recipeDto);

        response.Should().Be(1);
    }

    [Fact]
    public async Task UpdateRecipe_DatabaseContainsUpdatedRecipe_WhenRecipeIsUpdated()
    {
        var sut = _fixture.Sut;
        var baseRecipesList = await sut.GetRecipes();
        var recipeToUpdate = baseRecipesList.First();
        var recipeDto = new UpdateRecipeDto
        {
            Uuid = recipeToUpdate.Uuid,
            Title = new Bogus.DataSets.Lorem().Sentence(2)
        };

        await sut.UpdateRecipe(recipeDto);
        var updatedRecipesList = await sut.GetRecipes();

        updatedRecipesList.Should()
            .HaveCount(baseRecipesList.Count)
            .And
            .NotContain(recipeToUpdate)
            .And
            .ContainEquivalentOf(recipeDto);
    }
    
    [Fact]
    public async Task UpdateRecipe_ThrowsMySqlException_WhenDatabaseReturnsException()
    {
        var sut = new RecipeRepository(_fixture.MysqlTestContextWithoutSchema, _fixture.Logger);
        var recipeDto = new UpdateRecipeDto
        {
            Uuid = Guid.NewGuid(),
            Title = new Bogus.DataSets.Lorem().Sentence(2)
        };

        await sut.Invoking(r => r
            .UpdateRecipe(recipeDto)).Should().ThrowAsync<MySqlException>();
    }
    
    [Fact]
    public async Task UpdateRecipe_ThrowsRecipeDoesNotExistException_WhenRecipeToUpdateDoesNotExist()
    {
        var sut = _fixture.Sut;
        var recipeDto = new UpdateRecipeDto
        {
            Uuid = Guid.NewGuid(),
            Title = new Bogus.DataSets.Lorem().Sentence(2)
        };

        await sut.Invoking(r => r
            .UpdateRecipe(recipeDto)).Should().ThrowAsync<RecipeDoesNotExistException>();
    }
}