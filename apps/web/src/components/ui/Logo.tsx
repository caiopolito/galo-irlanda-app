import logo160 from "#/assets/galo-irlanda-logo-160.webp";
import logo512 from "#/assets/galo-irlanda-logo-512.webp";
import { cx } from "#/lib/cx";

interface LogoProps {
	size?: number;
	/** Use quando a logo estiver ao lado do nome "Galo Irlanda" escrito. */
	decorative?: boolean;
	className?: string;
}

/** Logo oficial da torcida (galo com o pint, em círculo, como no Instagram). */
export function Logo({ size = 44, decorative = false, className }: LogoProps) {
	return (
		<img
			src={size > 80 ? logo512 : logo160}
			srcSet={`${logo160} 160w, ${logo512} 512w`}
			sizes={`${size}px`}
			width={size}
			height={size}
			alt={decorative ? "" : "Galo Irlanda"}
			className={cx(className)}
			style={{ flex: "none", borderRadius: "50%" }}
		/>
	);
}
