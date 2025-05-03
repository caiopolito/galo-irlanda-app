import { Outlet } from 'react-router'
import { Box } from '@mui/material'

export const MainLayout = () => {
  return (
    <Box
      display="flex"
      minHeight="100vh"
      alignItems="center"
      justifyContent="center"
    >
      <Outlet />
    </Box>
  )
}