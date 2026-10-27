/** "€ 15" ou "€ 15,50". */
export function formatMoney(cents: number, currency = "EUR") {
	return new Intl.NumberFormat("pt-BR", {
		style: "currency",
		currency: currency.toUpperCase(),
		minimumFractionDigits: cents % 100 === 0 ? 0 : 2,
	}).format(cents / 100);
}
