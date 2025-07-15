import * as React from 'react';
import MuiAppBar from '@mui/material/AppBar';
import Link from '@mui/material/Link';
import Toolbar from '../toolbar/Toolbar';
import { ReactComponent as Logo } from 'assets/atletico.svg';
import { Box } from '@mui/material';

export const AppBar = ()  => {
  return (
    <div>
      <MuiAppBar elevation={0} position="fixed">
        <Toolbar sx={{ justifyContent: 'space-between' }}>
          <Link
            variant="h6"
            underline="none"
            color="inherit"
            sx={{ fontSize: 24 }}
            href="/#"
          >
            <Box sx={{ marginTop: 10}}>
              <Logo width={100} />
            </Box>
          </Link>
        </Toolbar>
      </MuiAppBar>
      <Toolbar />
    </div>
  );
};