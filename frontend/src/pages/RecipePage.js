import React, { useState } from 'react';

const RecipePage = () => {
    const [recipes, setRecipes] = useState([]);
    const [recipeData, setRecipeData] = useState({
        userId: '',
        dishId: '',
        name: '',
        description: '',
        timeTaken: '',
        ingredients: '',
        instructions: '',
        tags: '',
        mediaUrl: '',
        visibility: 'public',
        upvotes: 0,
        downvotes: 0,
    });
    const [responseMessage, setResponseMessage] = useState('');

    const fetchRecipes = async () => {
        try {
            const response = await fetch('http://localhost:5135/api/Recipes', {
                headers: {
                    'SessionId': 'mock-session-id', // Replace with an actual or mock session ID
                },
            });
            const data = await response.json();
            setRecipes(data);
        } catch (error) {
            console.error('Error fetching recipes:', error);
        }
    };

    const createRecipe = async () => {
        try {
            const response = await fetch('http://localhost:5135/api/Recipes', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'SessionId': 'mock-session-id', // Replace with an actual or mock session ID
                },
                body: JSON.stringify(recipeData),
            });
            if (!response.ok) {
                throw new Error('Failed to create recipe');
            }
            const data = await response.json();
            setResponseMessage(`Recipe created with ID: ${data.recipeId}`);
            fetchRecipes(); // Refresh the list
        } catch (error) {
            console.error('Error creating recipe:', error);
            setResponseMessage('Failed to create recipe');
        }
    };

    return (
        <div style={{ padding: '20px' }}>
            <h1>Recipe Page</h1>

            {/* Fetch Recipes Section */}
            <section>
                <h2>Fetch Recipes</h2>
                <button onClick={fetchRecipes}>Get Recipes</button>
                <ul>
                    {recipes.map((recipe) => (
                        <li key={recipe.recipeId}>
                            <strong>{recipe.name}</strong> - {recipe.description}
                        </li>
                    ))}
                </ul>
            </section>

            {/* Create Recipe Section */}
            <section>
                <h2>Create Recipe</h2>
                <input
                    type="text"
                    placeholder="User ID"
                    value={recipeData.userId}
                    onChange={(e) => setRecipeData({ ...recipeData, userId: e.target.value })}
                />
                <input
                    type="text"
                    placeholder="Dish ID"
                    value={recipeData.dishId}
                    onChange={(e) => setRecipeData({ ...recipeData, dishId: e.target.value })}
                />
                <input
                    type="text"
                    placeholder="Name"
                    value={recipeData.name}
                    onChange={(e) => setRecipeData({ ...recipeData, name: e.target.value })}
                />
                <textarea
                    placeholder="Description"
                    value={recipeData.description}
                    onChange={(e) => setRecipeData({ ...recipeData, description: e.target.value })}
                ></textarea>
                <input
                    type="number"
                    placeholder="Time Taken (minutes)"
                    value={recipeData.timeTaken}
                    onChange={(e) => setRecipeData({ ...recipeData, timeTaken: e.target.value })}
                />
                <textarea
                    placeholder="Ingredients"
                    value={recipeData.ingredients}
                    onChange={(e) => setRecipeData({ ...recipeData, ingredients: e.target.value })}
                ></textarea>
                <textarea
                    placeholder="Instructions"
                    value={recipeData.instructions}
                    onChange={(e) => setRecipeData({ ...recipeData, instructions: e.target.value })}
                ></textarea>
                <input
                    type="text"
                    placeholder="Tags (comma-separated)"
                    value={recipeData.tags}
                    onChange={(e) => setRecipeData({ ...recipeData, tags: e.target.value })}
                />
                <input
                    type="url"
                    placeholder="Media URL"
                    value={recipeData.mediaUrl}
                    onChange={(e) => setRecipeData({ ...recipeData, mediaUrl: e.target.value })}
                />
                <select
                    value={recipeData.visibility}
                    onChange={(e) => setRecipeData({ ...recipeData, visibility: e.target.value })}
                >
                    <option value="public">Public</option>
                    <option value="private">Private</option>
                    <option value="friends-only">Friends Only</option>
                </select>
                <button onClick={createRecipe}>Create Recipe</button>
                <p>{responseMessage}</p>
            </section>
        </div>
    );
};

export default RecipePage;
