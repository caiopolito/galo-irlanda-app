import { Outlet } from 'react-router'
import { Box } from '@mui/material'
import { AppBar } from 'components/appbar/AppBar'
import { AppFooter } from 'components/footer/Footer';
import { styled } from '@mui/material/styles'

const LayoutRoot = styled('section')(({ theme }) => ({
  color: theme.palette.common.black,
  position: 'relative',
  display: 'flex',
  alignItems: 'center',
  overflow:'hidden',
  justifyContent: 'space-evenly',
  // [theme.breakpoints.up('sm')]: {
  //   height: '80vh',
  //   minHeight: 500,
  //   maxHeight: 1300,
  // },
}));

const MainLayout = () => {
  return (
    <Box>
      <AppBar />
      <LayoutRoot>
        <Outlet />
      </LayoutRoot>
      <AppFooter />
    </Box>
  )
}

export default MainLayout;