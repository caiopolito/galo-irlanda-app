import * as React from 'react';
import { styled } from '@mui/material/styles';
import Box from '@mui/material/Box';
import ButtonBase from '@mui/material/ButtonBase';
import Container from '@mui/material/Container';
import Typography from 'components/typography/Typography';
import FirstImage from '../../../../assets/galo-irlanda-1.jpg';
import SecondImage from '../../../../assets/galo-irlanda-2.jpg';
import ThirdImage from '../../../../assets/galo-irlanda-3.jpg';
import FourthImage from '../../../../assets/galo-irlanda-4.webp';
import FifthImage from '../../../../assets/galo-irlanda-5.jpg';
import SixthImage from '../../../../assets/galo-irlanda-6.webp';
import { Link } from '@mui/material';

const ImageBackdrop = styled('div')(({ theme }) => ({
  position: 'absolute',
  left: 0,
  right: 0,
  top: 0,
  bottom: 0,
  background: '#000',
  opacity: 0.5,
  transition: theme.transitions.create('opacity'),
}));

const ImageIconButton = styled(ButtonBase)(({ theme }) => ({
  position: 'relative',
  display: 'block',
  padding: 0,
  borderRadius: 0,
  height: '40vh',
  [theme.breakpoints.down('md')]: {
    width: '100% !important',
    height: 100,
  },
  '&:hover': {
    zIndex: 1,
  },
  '&:hover .imageBackdrop': {
    opacity: 0.15,
  },
  '&:hover .imageMarked': {
    opacity: 0,
  },
  '&:hover .imageTitle': {
    border: '4px solid currentColor',
  },
  '& .imageTitle': {
    position: 'relative',
    padding: `${theme.spacing(2)} ${theme.spacing(4)} 14px`,
  },
  '& .imageMarked': {
    height: 3,
    width: 18,
    background: theme.palette.common.white,
    position: 'absolute',
    bottom: -2,
    left: 'calc(50% - 9px)',
    transition: theme.transitions.create('opacity'),
  },
}));

const images = [
  {
    imageUrl: `${FirstImage}`,
    title: '1º Churrasco 2025',
    width: '40%',
    url: `https://www.instagram.com/p/DI9zy-Qomvo/?img_index=1`
  },
  {
    imageUrl: `${SecondImage}`,
    title: 'UNIÃO SINISTRA',
    width: '20%',
    url: `https://www.instagram.com/p/DDXoMD2IFUC/?img_index=1`
  },
  {
    imageUrl: `${ThirdImage}`,
    title: 'FINAL COPA DO BRASIL 2024',
    width: '40%',
    url: `https://www.instagram.com/p/DCPdBGYIlKk/?img_index=1`
  },
  {
    imageUrl: `${FourthImage}`,
    title: 'FINAL CAMPEONATO MINEIRO 2025',
    width: '38%',
    url: `https://www.instagram.com/p/DHPEja3qmrx/`
  },
  {
    imageUrl: `${FifthImage}`,
    title: '1º CHURRASCO 2024',
    width: '28%',
    url: `https://www.instagram.com/p/C9KSyCvoGNG/?img_index=1`
  },
  {
    imageUrl: `${SixthImage}`,
    title: 'SEMIFINAL COPA DO BRASIL 2024',
    width: '34%',
    url: `https://www.instagram.com/p/DAqjG_6oUkJ/?img_index=1`
  }
];

export default function HomeCategories() {
  return (
    <Container component="section" sx={{ mt: 8, mb: 4 }}>
      <Typography variant="h4" marked="center" align="center" component="h2">
        VEJA NOSSOS ÚLTIMOS ENCONTROS!
      </Typography>
      <Box sx={{ mt: 8, display: 'flex', flexWrap: 'wrap' }}>
        {images.map((image) => (
            <ImageIconButton
              key={image.title}
              style={{
                width: image.width,
              }}
            >
              <Link href={image.url}>
              <Box
                sx={{
                  position: 'absolute',
                  left: 0,
                  right: 0,
                  top: 0,
                  bottom: 0,
                  backgroundSize: 'cover',
                  backgroundPosition: 'center 40%',
                  backgroundImage: `url(${image.imageUrl})`,
                }}
              />
              <ImageBackdrop className="imageBackdrop" />
              <Box
                sx={{
                  position: 'absolute',
                  left: 0,
                  right: 0,
                  top: 0,
                  bottom: 0,
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  color: 'common.white',
                }}
              >
                <Typography
                  component="h3"
                  variant="h6"
                  color="inherit"
                  className="imageTitle"
                >
                  {image.title}
                  <div className="imageMarked" />
                </Typography>
              </Box>
              </Link>
            </ImageIconButton>
        ))}
      </Box>
    </Container>
  );
}
