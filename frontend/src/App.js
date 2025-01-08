import React from 'react';
import { BrowserRouter as Router, Route, Routes } from 'react-router-dom';
import RecipePage from './pages/RecipePage';

const App = () => {
    return (
        <Router>
            <div>
                <header style={{ padding: '10px', background: '#f8f8f8', marginBottom: '20px' }}>
                    <h1 style={{ textAlign: 'center' }}>Cold Storage GO!</h1>
                </header>
                <main>
                    <Routes>
                        {/* Add the route for RecipePage */}
                        <Route path="/recipes" element={<RecipePage />} />
                        {/* Default route or other pages can go here */}
                        <Route
                            path="/"
                            element={
                                <div style={{ textAlign: 'center', padding: '20px' }}>
                                    <h2>Welcome to Cold Storage GO!</h2>
                                    <p>Navigate to <a href="/recipes">Recipe Page</a> to test APIs.</p>
                                </div>
                            }
                        />
                    </Routes>
                </main>
            </div>
        </Router>
    );
};

export default App;
