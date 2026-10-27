import { useEffect, useState } from "react";

/** Hora atual, atualizada a cada `intervalMs`. `null` até montar no navegador, para não divergir do HTML gerado. */
export function useNow(intervalMs = 30_000) {
	const [now, setNow] = useState<Date | null>(null);

	useEffect(() => {
		setNow(new Date());
		const id = setInterval(() => setNow(new Date()), intervalMs);
		return () => clearInterval(id);
	}, [intervalMs]);

	return now;
}
