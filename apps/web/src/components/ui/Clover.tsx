import { cx } from "#/lib/cx";

interface CloverProps {
	/** 3 folhas = shamrock, símbolo da Irlanda (padrão); 4 = trevo da sorte. */
	leaves?: 3 | 4;
	/** Cheio para tamanhos pequenos (≤ 20 px); traço à mão para maiores. */
	filled?: boolean;
	size?: number;
	className?: string;
	style?: React.CSSProperties;
	/** Texto para leitores de tela; sem ele o trevo é decorativo. */
	title?: string;
}

const LEAVES_3 = [
	{
		rotate: 0,
		d: "M0 0C-1.5-2.5-7.5-4.5-7.5-9C-7.5-12.5-3-13.5 0-10C3-13.8 7.8-12 7.2-8.6C6.8-5 1.8-2.8 0 0Z",
	},
	{
		rotate: -116,
		d: "M0 0C-2-3-7-4-7.2-8.8C-7.4-12.2-3.2-13.2 0-9.6C2.6-13.4 7.4-12.6 7.4-9.2C7.4-5.4 2.4-3 0 0Z",
	},
	{
		rotate: 114,
		d: "M0 0C-1.8-2.8-7.8-5-7.4-9.2C-7-12.6-2.6-13 0-9.8C3.2-13 7.2-11.8 7-8.8C6.8-5.2 1.6-2.4 0 0Z",
	},
];

const LEAVES_4 = [
	{ rotate: -44, d: LEAVES_3[0].d },
	{ rotate: 46, d: LEAVES_3[1].d },
	{ rotate: 134, d: LEAVES_3[2].d },
	{
		rotate: -136,
		d: "M0 0C-1.6-2.6-7.2-4.6-7.4-9C-7.6-12.4-3-13.4 0-9.9C2.9-13.4 7.6-12.2 7.2-8.8C6.9-5.1 1.8-2.6 0 0Z",
	},
];

/** Trevo desenhado à mão, no traço das anotações do layout. Usa `currentColor`. */
export function Clover({
	leaves = 3,
	filled = false,
	size = 24,
	className,
	style,
	title,
}: CloverProps) {
	const four = leaves === 4;
	const shape = (
		<g
			transform={four ? "translate(16 14.5) scale(.86)" : "translate(16 14)"}
			fill={filled ? "currentColor" : "none"}
			stroke="currentColor"
			strokeWidth={filled ? 1.2 : 2}
			strokeLinejoin="round"
			strokeLinecap="round"
		>
			{(four ? LEAVES_4 : LEAVES_3).map((leaf) => (
				<path
					key={leaf.rotate}
					transform={`rotate(${leaf.rotate})`}
					d={leaf.d}
				/>
			))}
			<path
				fill="none"
				strokeWidth={filled ? 2.4 : 2}
				d={four ? "M0 1C-.4 7 .6 12 3.4 17" : "M.2 1.5C.8 7 2.4 11.6 5.6 15.4"}
			/>
		</g>
	);
	const common = {
		width: size,
		height: size,
		viewBox: "0 0 32 32",
		className: cx(className),
		style: { flex: "none", ...style },
	};

	return title ? (
		<svg {...common} role="img">
			<title>{title}</title>
			{shape}
		</svg>
	) : (
		<svg {...common} aria-hidden="true">
			{shape}
		</svg>
	);
}
