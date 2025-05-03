import { createTheme, responsiveFontSizes } from '@mui/material/styles'

export const colors = {
  primary: {
    main: '#000',
  },
  secondary: {
    main: '#fff',
  }
}


export const customTheme = () => {
  const theme = createTheme({
    palette: {
      ...colors,
    }
  })

  return responsiveFontSizes(theme)
}

export default customTheme
