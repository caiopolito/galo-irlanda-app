import * as React from 'react';
import Box from '@mui/material/Box';
import Grid from '@mui/material/GridLegacy';
import Link from '@mui/material/Link';
import Container from '@mui/material/Container';
import Typography from 'components/typography/Typography';
import TextField from 'components/textfield/TextField';

function Copyright() {
  return (
    <React.Fragment>
      {'© '}
      <Link color="inherit" href="https://galoirlanda.com/#">
        Galo Irlanda
      </Link>{' '}
      {new Date().getFullYear()}
    </React.Fragment>
  );
}

const iconStyle = {
  width: 48,
  height: 48,
  display: 'flex',
  justifyContent: 'center',
  alignItems: 'center',
  backgroundColor: 'warning.main',
  mr: 1,
  '&:hover': {
    bgcolor: 'warning.dark',
  },
};

const LANGUAGES = [
  {
    code: 'en-US',
    name: 'English',
  },
  {
    code: 'pt-BR',
    name: 'Português (Brasil)'
  }
//   {
//     code: 'fr-FR',
//     name: 'Français',
//   },
];

export const AppFooter = () => {
  return (
    <Typography
      component="footer"
      sx={{ display: 'flex', bgcolor: 'primary.main', color: 'secondary.main' }}
    >
      <Container sx={{ my: 8, display: 'flex' }}>
        {/* <Grid container spacing={5}>
          <Grid item xs={6} sm={4} md={3}>
            <Grid
              container
              direction="column"
              spacing={2}
              sx={{ justifyContent: 'flex-end', height: 120 }}
            >
              <Grid item sx={{ display: 'flex' }}>
              </Grid>
              <Grid item>
                <Copyright />
              </Grid>
            </Grid>
          </Grid>
          <Grid item xs={6} sm={4} md={2}>
            <Typography variant="h6" marked="left" gutterBottom color="inherit">
              Legal
            </Typography>
            <Box component="ul" sx={{ m: 0, listStyle: 'none', p: 0 }}>
              <Box component="li" sx={{ py: 0.5 }}>
                <Link href="/#" color="inherit">Terms</Link>
              </Box>
              <Box component="li" sx={{ py: 0.5 }}>
                <Link href="/#" color="inherit">Privacy</Link>
              </Box>
            </Box>
          </Grid>
          <Grid item xs={6} sm={8} md={4}>
            <Typography variant="h6" marked="left" gutterBottom color="inherit">
              Language
            </Typography>
            <TextField
              select
              size="medium"
              variant="standard"
              SelectProps={{
                native: true,
              }}
              sx={{ mt: 1, width: 150, color: 'secondary.main' }}
            >
              {LANGUAGES.map((language) => (
                <option value={language.code} key={language.code}>
                  {language.name}
                </option>
              ))}
            </TextField>
          </Grid>
          <Grid item>
          </Grid>
        </Grid> */}
      </Container>
    </Typography>
  );
}
