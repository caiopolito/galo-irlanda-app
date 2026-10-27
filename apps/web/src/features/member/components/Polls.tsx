import type { Schemas } from "@galo/api-client";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import { ApiError, api, unwrap } from "#/lib/api";
import styles from "../member.module.css";
import { memberKeys } from "../useMember";

type Poll = Schemas["ListMemberPollsItem"];

/** Em votação secreta a API não sabe o voto: lembramos só neste aparelho para mostrar o "✓ seu voto". */
const localVoteKey = (pollId: string) => `galo-voto-${pollId}`;

function readLocalVote(pollId: string) {
	try {
		return localStorage.getItem(localVoteKey(pollId));
	} catch {
		return null;
	}
}

function closesIn(iso: string) {
	const days = Math.ceil((new Date(iso).getTime() - Date.now()) / 86_400_000);
	if (days <= 0) return "fecha hoje";
	return days === 1 ? "fecha amanhã" : `fecha em ${days} dias`;
}

function PollCard({ poll }: { poll: Poll }) {
	const queryClient = useQueryClient();
	const [mine, setMine] = useState<string | null>(poll.myOptionId);
	useEffect(() => {
		if (!poll.myOptionId) setMine(readLocalVote(poll.id));
	}, [poll.id, poll.myOptionId]);

	const vote = useMutation({
		mutationFn: (optionId: string) =>
			unwrap(
				api.POST("/api/socio/polls/{id}/vote", {
					params: { path: { id: poll.id } },
					body: { optionId },
				}),
			),
		onSuccess: (_, optionId) => {
			try {
				localStorage.setItem(localVoteKey(poll.id), optionId);
			} catch {
				// sem armazenamento local: o voto vale, só não mostramos o ✓
			}
			setMine(optionId);
			return queryClient.invalidateQueries({ queryKey: memberKeys.polls });
		},
	});
	const canVote = poll.isOpen && !poll.hasVoted;
	const total = poll.totalVotes ?? 0;
	const error = vote.error instanceof ApiError ? vote.error : null;

	return (
		<div className={styles.paperPanel}>
			<span className={styles.eyebrow}>
				Votação da torcida ·{" "}
				{poll.isOpen ? closesIn(poll.closesAt) : "encerrada"}
				{poll.isSecret && " · voto secreto"}
			</span>
			<h2 className={styles.h2}>{poll.title}</h2>
			{poll.description && <p style={{ margin: 0 }}>{poll.description}</p>}
			<div className={styles.pollOptions}>
				{poll.options.map((option) => {
					const percent =
						total > 0 && option.votes != null
							? Math.round((option.votes / total) * 100)
							: 0;
					const isMine = option.id === mine;
					const content = (
						<>
							{option.votes != null && (
								<span
									className={`${styles.pollBar} ${isMine ? styles.pollBarMine : ""}`}
									style={{ width: `${percent}%` }}
								/>
							)}
							<span className={styles.pollLabel}>
								<span>
									{option.label}
									{isMine && " ✓ seu voto"}
								</span>
								{option.votes != null && <span>{percent}%</span>}
							</span>
						</>
					);
					return canVote ? (
						<button
							key={option.id}
							type="button"
							className={styles.pollOption}
							disabled={vote.isPending}
							onClick={() =>
								confirm(
									`Votar em “${option.label}”? Não dá para mudar depois.`,
								) && vote.mutate(option.id)
							}
						>
							{content}
						</button>
					) : (
						<div key={option.id} className={styles.pollOption}>
							{content}
						</div>
					);
				})}
			</div>
			{canVote && (
				<span className={styles.eyebrow}>
					Toque numa opção para votar. O resultado aparece depois.
				</span>
			)}
			{error && (
				<span style={{ color: "#a3261a", fontWeight: 700 }}>
					{error.summary}
				</span>
			)}
			{poll.handNote && (
				<span className={styles.handNoteDark}>{poll.handNote}</span>
			)}
		</div>
	);
}

export function Polls() {
	const polls = useQuery({
		queryKey: memberKeys.polls,
		queryFn: () => unwrap(api.GET("/api/socio/polls")),
	});

	return (
		<section
			id="votacao"
			style={{ display: "flex", flexDirection: "column", gap: 16 }}
		>
			{polls.data?.length === 0 && (
				<div className={styles.paperPanel}>
					<span className={styles.eyebrow}>Votação da torcida</span>
					<p style={{ margin: 0 }}>Nenhuma votação aberta agora.</p>
				</div>
			)}
			{polls.data?.map((poll) => (
				<PollCard key={poll.id} poll={poll} />
			))}
		</section>
	);
}
