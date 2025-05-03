import React from 'react'
import { Routes, Route, HashRouter, BrowserRouter } from 'react-router'
import LandingRoute from './routes/app/landing'
import NotFoundRoute from './routes/not-found';
import { MainLayout } from './layouts/MainLayout';

export const AppRouter = () => {
  return (
    <BrowserRouter>
      <Routes>
          <Route path="/" element={<MainLayout />}>
            <Route index element={<LandingRoute />}/>
            <Route path="*" element={<NotFoundRoute />}/>
          </Route>
      </Routes>
    </BrowserRouter>
  );
};
