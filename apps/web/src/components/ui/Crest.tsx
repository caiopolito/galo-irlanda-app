import crestUrl from "#/assets/atletico.svg";

// Proporção do viewBox do arquivo oficial (322.7 × 480.1).
const RATIO = 322.7 / 480.1;

interface CrestProps {
	height?: number;
	/** Use quando o escudo estiver ao lado de um texto que já diz o que ele é. */
	decorative?: boolean;
	className?: string;
}

/** Escudo oficial do Clube Atlético Mineiro (ADR 0012). Único ponto do código que usa o arquivo. */
export function Crest({
	height = 40,
	decorative = false,
	className,
}: CrestProps) {
	return (
		<img
			src={crestUrl}
			alt={decorative ? "" : "Escudo do Clube Atlético Mineiro"}
			height={height}
			width={Math.round(height * RATIO)}
			className={className}
		/>
	);
}
