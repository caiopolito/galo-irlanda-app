import { styled } from '@mui/material/styles';
import MuiToolbar from '@mui/material/Toolbar';

const Toolbar = styled(MuiToolbar)(({ theme }) => ({
  height: 90,
  [theme.breakpoints.up('sm')]: {
    height: 96,
  },
}));

export default Toolbar;
