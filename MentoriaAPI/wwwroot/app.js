const state = {
  token: "",
  user: null,
  students: [],
  mentors: [],
  tutors: [],
  matches: [],
  sessions: [],
  feedback: [],
  tutoring: null,
  metrics: null,
  editingSessionId: null
};

const elements = {
  loginForm: document.querySelector("#login-form"),
  email: document.querySelector("#email"),
  password: document.querySelector("#password"),
  loginStatus: document.querySelector("#login-status"),
  appShell: document.querySelector("#app-shell"),
  menuTabs: document.querySelectorAll(".menu-tab"),
  viewPanels: document.querySelectorAll(".view-panel"),
  welcomeTitle: document.querySelector("#welcome-title"),
  sessionSubtitle: document.querySelector("#session-subtitle"),
  roleBadge: document.querySelector("#role-badge"),
  metricsGrid: document.querySelector("#metrics-grid"),
  studentsList: document.querySelector("#students-list"),
  mentorsList: document.querySelector("#mentors-list"),
  tutorsList: document.querySelector("#tutors-list"),
  matchesList: document.querySelector("#matches-list"),
  tutoringOverview: document.querySelector("#tutoring-overview"),
  sessionsList: document.querySelector("#sessions-list"),
  feedbackList: document.querySelector("#feedback-list"),
  summaryHighlights: document.querySelector("#summary-highlights"),
  activityTimeline: document.querySelector("#activity-timeline"),
  recommendationBox: document.querySelector("#recommendation-box"),
  studentSelect: document.querySelector("#student-select"),
  matchSelect: document.querySelector("#match-select"),
  feedbackSessionSelect: document.querySelector("#feedback-session-select"),
  feedbackAuthorSelect: document.querySelector("#feedback-author-select"),
  feedbackTargetSelect: document.querySelector("#feedback-target-select"),
  matchForm: document.querySelector("#match-form"),
  sessionForm: document.querySelector("#session-form"),
  feedbackForm: document.querySelector("#feedback-form"),
  sessionDate: document.querySelector("#session-date"),
  sessionDuration: document.querySelector("#session-duration"),
  sessionTopic: document.querySelector("#session-topic"),
  sessionNotes: document.querySelector("#session-notes"),
  sessionSubmitButton: document.querySelector("#session-submit-button"),
  sessionCancelEdit: document.querySelector("#session-cancel-edit"),
  sessionFormStatus: document.querySelector("#session-form-status"),
  feedbackFormStatus: document.querySelector("#feedback-form-status"),
  refreshButton: document.querySelector("#refresh-dashboard"),
  logoutButton: document.querySelector("#logout-button"),
  quickGenerateMatch: document.querySelector("#quick-generate-match"),
  quickDistributeTutoring: document.querySelector("#quick-distribute-tutoring"),
  quickCompleteSession: document.querySelector("#quick-complete-session"),
  quickExportState: document.querySelector("#quick-export-state"),
  phaseSelect: document.querySelector("#phase-select"),
  quickActionStatus: document.querySelector("#quick-action-status")
};

document.querySelectorAll(".credential-card").forEach(button => {
  button.addEventListener("click", () => {
    elements.email.value = button.dataset.email ?? "";
    elements.password.value = button.dataset.password ?? "";
  });
});

elements.loginForm.addEventListener("submit", async (event) => {
  event.preventDefault();
  elements.loginStatus.textContent = "A autenticar...";

  try {
    const payload = await fetchJson("/api/auth/login", {
      method: "POST",
      body: JSON.stringify({
        email: elements.email.value.trim(),
        password: elements.password.value
      })
    });

    state.token = payload.token;
    state.user = payload.user;
    const roles = payload.user.roles ?? [payload.user.role ?? "User"];
    elements.welcomeTitle.textContent = `Dashboard de ${payload.user.name}`;
    elements.roleBadge.textContent = roles.join(" / ");
    elements.sessionSubtitle.textContent = getRoleNarrative(roles);
    elements.loginStatus.textContent = "";
    elements.appShell.classList.remove("hidden");
    configureRoleView();
    setActiveView("overview-view");

    await loadDashboard();
  } catch (error) {
    elements.loginStatus.textContent = error.message;
  }
});

elements.refreshButton.addEventListener("click", loadDashboard);
elements.menuTabs.forEach(tab => {
  tab.addEventListener("click", () => setActiveView(tab.dataset.view));
});
document.addEventListener("click", (event) => {
  const tab = event.target.closest?.(".menu-tab");
  if (tab?.dataset.view) {
    setActiveView(tab.dataset.view);
    return;
  }

  const completeButton = event.target.closest?.("[data-complete-session]");
  if (completeButton) {
    completeSession(Number(completeButton.dataset.completeSession));
    return;
  }

  const cancelButton = event.target.closest?.("[data-cancel-session]");
  if (cancelButton) {
    cancelSession(Number(cancelButton.dataset.cancelSession));
    return;
  }

  const rescheduleButton = event.target.closest?.("[data-reschedule-session]");
  if (rescheduleButton) {
    startRescheduleSession(Number(rescheduleButton.dataset.rescheduleSession));
  }
});
elements.feedbackSessionSelect.addEventListener("change", fillFeedbackParticipants);
elements.feedbackAuthorSelect.addEventListener("change", fillFeedbackTargetOptions);
elements.sessionCancelEdit.addEventListener("click", resetSessionForm);
elements.logoutButton.addEventListener("click", () => {
  state.token = "";
  state.user = null;
  elements.appShell.classList.add("hidden");
  elements.loginStatus.textContent = "Sessão terminada.";
});

elements.quickGenerateMatch.addEventListener("click", async () => {
  if (!elements.studentSelect.value) {
    elements.quickActionStatus.textContent = "Escolhe primeiro um aluno.";
    return;
  }

  try {
    await generateMatchForSelectedStudent();
    elements.quickActionStatus.textContent = "Emparelhamento gerado com sucesso.";
  } catch (error) {
    elements.quickActionStatus.textContent = error.message;
  }
});

elements.quickDistributeTutoring.addEventListener("click", async () => {
  try {
    const phase = elements.phaseSelect.value;
    state.tutoring = await api(`/api/tutoring/distribute/${phase}`, { method: "POST" });
    await loadDashboard();
    elements.quickActionStatus.textContent = `Distribuição da ${phase}.ª fase concluída.`;
  } catch (error) {
    elements.quickActionStatus.textContent = error.message;
  }
});

elements.quickCompleteSession.addEventListener("click", async () => {
  const scheduledSession = [...state.sessions]
    .filter(session => session.status !== "Completed")
    .sort((left, right) => new Date(left.scheduledAt) - new Date(right.scheduledAt))[0];

  if (!scheduledSession) {
    elements.quickActionStatus.textContent = "Não existem sessões pendentes para concluir.";
    return;
  }

  try {
    await api(`/api/sessions/${scheduledSession.sessionId}/status`, {
      method: "PUT",
      body: JSON.stringify({
        status: "Completed",
        notes: `${scheduledSession.notes} Sessão concluída no modo apresentação.`
      })
    });
    await loadDashboard();
    elements.quickActionStatus.textContent = `Sessão "${scheduledSession.topic}" marcada como concluída.`;
  } catch (error) {
    elements.quickActionStatus.textContent = error.message;
  }
});

elements.quickExportState.addEventListener("click", () => {
  const payload = JSON.stringify({
    generatedAt: new Date().toISOString(),
    user: state.user,
    metrics: state.metrics,
    students: state.students,
    mentors: state.mentors,
    matches: state.matches,
    sessions: state.sessions,
    feedback: state.feedback,
    tutoring: state.tutoring
  }, null, 2);

  const blob = new Blob([payload], { type: "application/json" });
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement("a");
  anchor.href = url;
  anchor.download = "ptm-nonio-demo-state.json";
  anchor.click();
  URL.revokeObjectURL(url);
  elements.quickActionStatus.textContent = "Estado atual exportado em JSON.";
});

async function completeSession(sessionId) {
  const session = state.sessions.find(item => item.sessionId === sessionId);
  if (!session) {
    elements.quickActionStatus.textContent = "Sessão não encontrada.";
    return;
  }

  try {
    await api(`/api/sessions/${sessionId}/status`, {
      method: "PUT",
      body: JSON.stringify({
        status: "Completed",
        notes: session.notes || "Sessão marcada como ocorrida pelo mentor."
      })
    });
    await loadDashboard();
    setSessionStatus(`Sessão "${session.topic}" marcada como ocorrida.`);
  } catch (error) {
    setSessionStatus(error.message);
  }
}

async function cancelSession(sessionId) {
  const session = state.sessions.find(item => item.sessionId === sessionId);
  if (!session) {
    setSessionStatus("Sessão não encontrada.");
    return;
  }

  try {
    await api(`/api/sessions/${sessionId}/status`, {
      method: "PUT",
      body: JSON.stringify({
        status: "Cancelled",
        notes: session.notes || "Sessão cancelada pelo mentor."
      })
    });
    await loadDashboard();
    setSessionStatus(`Sessão "${session.topic}" cancelada.`);
  } catch (error) {
    setSessionStatus(error.message);
  }
}

function startRescheduleSession(sessionId) {
  const session = state.sessions.find(item => item.sessionId === sessionId);
  if (!session) {
    setSessionStatus("Sessão não encontrada.");
    return;
  }

  state.editingSessionId = sessionId;
  elements.matchSelect.value = String(session.matchId);
  elements.sessionDate.value = toDateTimeLocalValue(session.scheduledAt);
  elements.sessionDuration.value = session.durationMinutes;
  elements.sessionTopic.value = session.topic;
  elements.sessionNotes.value = session.notes || "";
  elements.sessionSubmitButton.textContent = "Guardar remarcação";
  elements.sessionCancelEdit.classList.remove("hidden");
  setSessionStatus(`A remarcar "${session.topic}".`);
  setActiveView("tracking-view");
}

elements.matchForm.addEventListener("submit", async (event) => {
  event.preventDefault();
  try {
    await generateMatchForSelectedStudent();
  } catch (error) {
    elements.recommendationBox.textContent = error.message;
  }
});

elements.sessionForm.addEventListener("submit", async (event) => {
  event.preventDefault();
  const payload = {
    matchId: Number(elements.matchSelect.value),
    scheduledAt: new Date(elements.sessionDate.value).toISOString(),
    durationMinutes: Number(elements.sessionDuration.value),
    topic: elements.sessionTopic.value.trim(),
    notes: elements.sessionNotes.value.trim()
  };

  try {
    const wasEditing = Boolean(state.editingSessionId);
    if (state.editingSessionId) {
      await api(`/api/sessions/${state.editingSessionId}/reschedule`, {
        method: "PUT",
        body: JSON.stringify(payload)
      });
    } else {
      await api("/api/sessions", {
        method: "POST",
        body: JSON.stringify(payload)
      });
    }

    resetSessionForm();
    await loadDashboard();
    setSessionStatus(wasEditing ? "Sessão remarcada com sucesso." : "Sessão criada com sucesso.");
  } catch (error) {
    setSessionStatus(error.message);
  }
});

elements.feedbackForm.addEventListener("submit", async (event) => {
  event.preventDefault();

  try {
    await api("/api/feedback", {
      method: "POST",
      body: JSON.stringify({
        sessionId: Number(elements.feedbackSessionSelect.value),
        authorUserId: Number(elements.feedbackAuthorSelect.value),
        targetUserId: Number(elements.feedbackTargetSelect.value),
        rating: Number(document.querySelector("#feedback-rating").value),
        comment: document.querySelector("#feedback-comment").value.trim()
      })
    });

    elements.feedbackForm.reset();
    await loadDashboard();
    setFeedbackStatus("Feedback registado com sucesso.");
  } catch (error) {
    setFeedbackStatus(error.message);
  }
});

async function generateMatchForSelectedStudent() {
  const studentId = elements.studentSelect.value;
  if (!studentId) {
    throw new Error("Escolhe um aluno antes de gerar o match.");
  }

  const recommendations = await api(`/api/matches/recommendations/${studentId}`);
  const top = recommendations[0];

  if (!top) {
    elements.recommendationBox.textContent = "Sem recomendação disponível para este aluno.";
    return;
  }

  const match = await api(`/api/matches/generate/${studentId}`, { method: "POST" });
  await loadDashboard();
  elements.studentSelect.value = String(studentId);
  elements.recommendationBox.textContent =
    `Emparelhamento ativo: ${match.student} → ${match.mentor} | score ${match.compatibilityScore} | ${match.summary}`;
}

async function loadDashboard() {
  const [metrics, students, mentors, tutors, matches, sessions, feedback, tutoring] = await Promise.all([
    api("/api/matches/metrics"),
    api("/api/students"),
    api("/api/mentors"),
    api("/api/tutors"),
    api("/api/matches"),
    api("/api/sessions"),
    api("/api/feedback"),
    api("/api/tutoring/overview")
  ]);

  state.metrics = metrics;
  state.students = students;
  state.mentors = mentors;
  state.tutors = tutors;
  state.matches = matches;
  state.sessions = sessions;
  state.feedback = feedback;
  state.tutoring = tutoring;

  renderMetrics();
  renderExecutiveSummary();
  renderTimeline();
  renderLists();
  fillSelects();
}

async function api(url, options = {}) {
  return fetchJson(url, {
    ...options,
    headers: {
      ...(state.token ? { Authorization: `Bearer ${state.token}` } : {}),
      ...(options.headers ?? {})
    }
  });
}

async function fetchJson(url, options = {}) {
  const response = await fetch(url, {
    ...options,
    headers: {
      "Content-Type": "application/json",
      ...(options.headers ?? {})
    }
  });

  if (!response.ok) {
    const text = await response.text();
    throw new Error(text || "Pedido falhou.");
  }

  const contentType = response.headers.get("content-type") ?? "";
  return contentType.includes("application/json") ? response.json() : response.text();
}

function renderMetrics() {
  const cards = isAdmin()
    ? [
      ["Utilizadores", state.metrics.totalUsers],
      ["Alunos", state.metrics.totalStudents],
      ["Mentores", state.metrics.totalMentors],
      ["Tutores", state.metrics.totalTutors],
      ["Tutorias ativas", state.metrics.totalTutoringAssignments],
      ["Sem mentor", state.metrics.assignmentsWithoutMentor],
      ["Matches", state.metrics.totalMatches],
      ["Matches ativos", state.metrics.activeMatches],
      ["Sessões", state.metrics.totalSessions],
      ["Feedback médio", state.metrics.averageFeedback]
    ]
    : [
      ["Tutorias visíveis", state.metrics.totalTutoringAssignments],
      ["Matches ativos", state.metrics.activeMatches],
      ["Sessões visíveis", state.metrics.totalSessions],
      ["Feedback médio", state.metrics.averageFeedback]
    ];

  elements.metricsGrid.innerHTML = cards.map(([label, value]) => `
    <article class="metric-card">
      <span>${label}</span>
      <strong>${value}</strong>
    </article>
  `).join("");
}

function renderExecutiveSummary() {
  const nextSession = [...state.sessions]
    .sort((left, right) => new Date(left.scheduledAt) - new Date(right.scheduledAt))[0];
  const topMatch = [...state.matches]
    .sort((left, right) => right.compatibilityScore - left.compatibilityScore)[0];
  const activeTutoring = state.tutoring?.activeAssignments ?? 0;
  const withoutMentor = state.tutoring?.withoutMentor ?? 0;
  const roleSummary = state.user?.roles?.join(", ") ?? state.user?.role ?? "Utilizador";

  const cards = [
    {
      title: "Utilizador ativo",
      text: `${state.user?.name ?? "-"} | ${roleSummary}`
    },
    {
      title: "Próxima sessão",
      text: nextSession
        ? `${nextSession.topic} em ${formatDate(nextSession.scheduledAt)}`
        : "Sem sessões agendadas."
    },
    {
      title: isAdmin() ? "Distribuição PTM" : "A minha distribuição",
      text: `${activeTutoring} atribuição(ões) ativa(s), ${withoutMentor} sem mentor por falta no curso.`
    },
    {
      title: "Melhor compatibilidade atual",
      text: topMatch
        ? `${topMatch.student} → ${topMatch.mentor} (${topMatch.compatibilityScore})`
        : "Ainda não existem matches."
    }
  ];

  elements.summaryHighlights.innerHTML = cards.map(card => `
    <article class="summary-card">
      <strong>${card.title}</strong>
      <p>${card.text}</p>
    </article>
  `).join("");
}

function renderTimeline() {
  const items = [
    ...state.matches.map(match => ({
      title: `Match ${match.student} → ${match.mentor}`,
      text: `Score ${match.compatibilityScore}. ${match.summary}`,
      date: new Date(match.createdAt),
      kind: "match"
    })),
    ...state.sessions.map(session => ({
      title: `Sessão: ${session.topic}`,
      text: session.status === "Busy"
        ? `${session.mentor} indisponível neste horário`
        : `${session.student} com ${session.mentor} | ${session.status}`,
      date: new Date(session.scheduledAt),
      kind: session.status === "Completed" ? "completed" : "scheduled"
    })),
    ...state.feedback.map(item => ({
      title: `Feedback ${item.author} → ${item.target}`,
      text: `${item.rating}/5 | ${item.comment || "Sem comentário."}`,
      date: new Date(item.createdAt),
      kind: "feedback"
    })),
    ...(state.tutoring?.assignments ?? []).map(item => ({
      title: `Tutoria ${item.student} → ${item.tutor}`,
      text: `${item.course} | mentor: ${item.mentor} | fase ${item.enrollmentPhase}`,
      date: new Date(item.assignedAt),
      kind: item.mentorId ? "match" : "scheduled"
    }))
  ]
    .sort((left, right) => right.date - left.date)
    .slice(0, 6);

  elements.activityTimeline.innerHTML = items.length
    ? items.map(item => `
      <article class="timeline-item ${item.kind === "scheduled" ? "warning" : ""}">
        <h4>${item.title}</h4>
        <p>${item.text}</p>
        <p>${formatDate(item.date)}</p>
      </article>
    `).join("")
    : `<article class="timeline-item"><h4>Sem atividade</h4><p>O sistema ainda não gerou eventos para mostrar.</p></article>`;
}

function renderLists() {
  elements.studentsList.innerHTML = state.students.length
    ? `
      <div class="people-table" role="table" aria-label="Perfis académicos dos alunos">
        <div class="people-row people-head" role="row">
          <span>Aluno</span>
          <span>Curso</span>
          <span>Interesses</span>
          <span>Média</span>
          <span>Estado</span>
        </div>
        ${state.students.map(student => `
          <div class="people-row" role="row">
            <span>
              <strong>${student.name}</strong>
              <small>${student.email}</small>
              <small>${student.goals}</small>
            </span>
            <span>${student.course} · ${student.academicYear}.º ano · F${student.enrollmentPhase}</span>
            <span>${student.areasOfInterest}</span>
            <span>${student.averageGrade}</span>
            <span class="${student.isEnrolled ? "status-ok" : "status-warning"}">${student.isEnrolled ? "Inscrito" : "Abandonou"}</span>
          </div>
        `).join("")}
      </div>
    `
    : `<article class="mini-card"><h4>Sem alunos visíveis</h4><p>Não existem alunos associados a esta sessão.</p></article>`;

  elements.mentorsList.innerHTML = state.mentors.map(mentor => `
    <article class="mini-card">
      <h4>${mentor.name}</h4>
      <p>${mentor.email}</p>
      <p>Curso: ${mentor.course} | Ano: ${mentor.academicYear}</p>
      <p>Especialização: ${mentor.specialization}</p>
      <p>Capacidade: ${mentor.currentStudents}/${mentor.maxStudents}</p>
      <div class="meta-row">
        <span class="pill">${mentor.areasOfExpertise}</span>
        <span class="pill">${mentor.yearsOfExperience} anos de experiência</span>
      </div>
    </article>
  `).join("");

  elements.tutorsList.innerHTML = state.tutors.length
    ? state.tutors.map(tutor => `
      <article class="mini-card">
        <h4>${tutor.name}</h4>
        <p>${tutor.email}</p>
        <p>Departamento: ${tutor.department}</p>
        <div class="meta-row">
          <span class="pill">Cursos: ${tutor.courses}</span>
          <span class="pill">Tutorandos: ${tutor.currentAssignments}</span>
        </div>
      </article>
    `).join("")
    : `<article class="mini-card"><h4>Sem tutores visíveis</h4><p>Não existem tutores associados a esta sessão.</p></article>`;

  elements.tutoringOverview.innerHTML = state.tutoring?.assignments?.length
    ? `
      <div class="assignment-table" role="table" aria-label="Distribuição oficial PTM">
        <div class="assignment-row assignment-head" role="row">
          <span>Aluno</span>
          <span>Mentor</span>
          <span>Tutor</span>
          <span>Curso</span>
          <span>Estado</span>
        </div>
        ${state.tutoring.assignments
          .filter(assignment => assignment.status === "Active")
          .map(assignment => `
            <div class="assignment-row" role="row">
              <span>
                <strong>${assignment.student}</strong>
                ${assignment.allocationWarning ? `<small class="warning-text">${assignment.allocationWarning}</small>` : ""}
              </span>
              <span>${assignment.mentor}</span>
              <span>${assignment.tutor}</span>
              <span>${assignment.course} · F${assignment.enrollmentPhase}</span>
              <span class="${assignment.mentorId ? "status-ok" : "status-warning"}">${assignment.mentorId ? "Mentor" : "Só tutor"}</span>
            </div>
          `).join("")}
      </div>
    `
    : `<article class="mini-card"><h4>Sem distribuição visível</h4><p>Ainda não existe atribuição PTM associada a esta conta.</p></article>`;

  elements.matchesList.innerHTML = state.matches.length
    ? state.matches
      .filter(match => match.status === "Active")
      .map(match => `
      <article class="mini-card">
        <h4>${match.student} → ${match.mentor}</h4>
        <p>${match.summary}</p>
        <div class="meta-row">
          <span class="pill">Score ${match.compatibilityScore}</span>
          <span class="pill ${match.status === "Active" ? "" : "warning"}">${match.status}</span>
        </div>
      </article>
    `).join("")
    : `<article class="mini-card"><h4>Sem matches</h4><p>Ainda não foram gerados emparelhamentos.</p></article>`;

  const visibleSessions = state.sessions
    .filter(session => session.status !== "Cancelled")
    .sort((left, right) => new Date(left.scheduledAt) - new Date(right.scheduledAt));
  elements.sessionsList.innerHTML = visibleSessions.length
    ? renderSessionGroups(visibleSessions)
    : `<article class="mini-card"><h4>Sem sessões</h4><p>A agenda ainda não foi preenchida.</p></article>`;

  elements.feedbackList.innerHTML = state.feedback.length
    ? state.feedback.map(item => `
      <article class="mini-card">
        <h4>${item.author} → ${item.target}</h4>
        <p>${item.comment || "Sem comentário."}</p>
        <div class="meta-row">
          <span class="pill">Rating ${item.rating}/5</span>
          <span class="pill">${formatDate(item.createdAt)}</span>
        </div>
      </article>
    `).join("")
    : `<article class="mini-card"><h4>Sem feedback</h4><p>Regista avaliações depois das sessões.</p></article>`;
}

function fillSelects() {
  const matchOptions = state.matches.filter(match => match.status === "Active");
  fillOptions(
    elements.studentSelect,
    isAdmin() ? state.students : state.students.filter(student => student.userId === state.user?.userId),
    item => item.userId,
    item => `${item.name} (${item.course} ${item.academicYear})`
  );
  fillOptions(
    elements.matchSelect,
    matchOptions,
    item => item.matchId,
    item => `${item.student} → ${item.mentor}`
  );
  fillOptions(
    elements.feedbackSessionSelect,
    state.sessions.filter(session => session.status === "Completed" && canLeaveFeedback(session)),
    item => item.sessionId,
    item => `${item.topic} - ${item.student}`
  );

  fillFeedbackParticipants();
}

function renderSessionGroups(sessions) {
  const groups = new Map();
  sessions.forEach(session => {
    const key = formatDateOnly(session.scheduledAt);
    groups.set(key, [...(groups.get(key) ?? []), session]);
  });

  return [...groups.entries()].map(([date, items]) => `
    <section class="session-day">
      <h4>${date}</h4>
      <div class="session-day-list">
        ${items.map(renderSessionCard).join("")}
      </div>
    </section>
  `).join("");
}

function renderSessionCard(session) {
  return `
    <article class="mini-card session-card">
      <h4>${session.topic}</h4>
      <p>${session.status === "Busy" ? `${session.mentor} não está disponível` : `${session.student} com ${session.mentor}`}</p>
      <p>${formatTimeRange(session)}</p>
      <div class="meta-row">
        <span class="pill ${session.status === "Completed" ? "" : "warning"}">${session.status === "Busy" ? "Indisponível" : session.status}</span>
        ${canCompleteSession(session) ? `<button type="button" class="secondary session-action" data-complete-session="${session.sessionId}">Marcar como ocorrida</button>` : ""}
        ${canManageScheduledSession(session) ? `<button type="button" class="secondary session-action" data-reschedule-session="${session.sessionId}">Remarcar</button>` : ""}
        ${canManageScheduledSession(session) ? `<button type="button" class="ghost session-action" data-cancel-session="${session.sessionId}">Cancelar</button>` : ""}
      </div>
    </article>
  `;
}

function fillFeedbackParticipants() {
  const session = state.sessions.find(item => String(item.sessionId) === String(elements.feedbackSessionSelect.value));
  const participants = session
    ? [...state.students, ...state.mentors].filter(user => user.userId === session.studentId || user.userId === session.mentorId)
    : [];

  const authorOptions = isAdmin() ? participants : participants.filter(item => item.userId === state.user?.userId);
  fillOptions(elements.feedbackAuthorSelect, authorOptions, item => item.userId, item => item.name);
  fillFeedbackTargetOptions();
}

function fillFeedbackTargetOptions() {
  const session = state.sessions.find(item => String(item.sessionId) === String(elements.feedbackSessionSelect.value));
  const participants = session
    ? [...state.students, ...state.mentors].filter(user => user.userId === session.studentId || user.userId === session.mentorId)
    : [];
  const authorId = Number(elements.feedbackAuthorSelect.value);
  const targetOptions = participants.filter(item => item.userId !== authorId);
  fillOptions(elements.feedbackTargetSelect, targetOptions, item => item.userId, item => item.name);
}

function fillOptions(select, items, valueFn, labelFn) {
  select.innerHTML = items.length
    ? items.map(item => `<option value="${valueFn(item)}">${labelFn(item)}</option>`).join("")
    : `<option value="">Sem opções disponíveis</option>`;
}

function formatDate(value) {
  return new Date(value).toLocaleString("pt-PT", {
    dateStyle: "short",
    timeStyle: "short"
  });
}

function formatDateOnly(value) {
  return new Date(value).toLocaleDateString("pt-PT", {
    weekday: "long",
    day: "2-digit",
    month: "short"
  });
}

function formatTimeRange(session) {
  const start = new Date(session.scheduledAt);
  const end = new Date(start.getTime() + session.durationMinutes * 60000);
  return `${start.toLocaleTimeString("pt-PT", { hour: "2-digit", minute: "2-digit" })} - ${end.toLocaleTimeString("pt-PT", { hour: "2-digit", minute: "2-digit" })} | ${session.durationMinutes} min`;
}

function getRoleNarrative(roles) {
  if (roles.includes("Admin")) {
    return "Vista de coordenação global para demonstração do ciclo completo da plataforma.";
  }

  if (roles.includes("Mentor")) {
    return "Vista de mentor focada em alunos atribuídos, agenda e feedback.";
  }

  if (roles.includes("Student")) {
    return "Vista de aluno com foco em acompanhamento, sessões e progresso.";
  }

  return "Vista geral da plataforma.";
}

function setActiveView(viewId) {
  const targetTab = [...elements.menuTabs].find(tab => tab.dataset.view === viewId);
  if (targetTab?.hidden) {
    viewId = "overview-view";
  }

  elements.menuTabs.forEach(tab => {
    tab.classList.toggle("active", tab.dataset.view === viewId);
  });
  elements.viewPanels.forEach(panel => {
    const active = panel.id === viewId;
    panel.classList.toggle("active", active);
    panel.hidden = !active;
  });
}

function configureRoleView() {
  const allowedViews = getAllowedViews();
  elements.menuTabs.forEach(tab => {
    tab.hidden = !allowedViews.includes(tab.dataset.view);
  });
}

function getAllowedViews() {
  if (isAdmin()) {
    return ["overview-view", "ptm-view", "actions-view", "people-view", "tracking-view"];
  }

  if (hasRole("Mentor")) {
    return ["overview-view", "ptm-view", "people-view", "tracking-view"];
  }

  if (hasRole("Student")) {
    return ["overview-view", "tracking-view"];
  }

  return ["overview-view"];
}

function hasRole(role) {
  return (state.user?.roles ?? [state.user?.role]).includes(role);
}

function isAdmin() {
  return hasRole("Admin");
}

function canCompleteSession(session) {
  return session.sessionId > 0 &&
    session.status === "Scheduled" &&
    (isAdmin() || hasRole("Mentor"));
}

function canManageScheduledSession(session) {
  return session.sessionId > 0 &&
    session.status === "Scheduled" &&
    (isAdmin() || hasRole("Mentor"));
}

function canLeaveFeedback(session) {
  if (isAdmin()) {
    return true;
  }

  return !state.feedback.some(item =>
    item.sessionId === session.sessionId &&
    item.authorUserId === state.user?.userId);
}

function resetSessionForm() {
  state.editingSessionId = null;
  elements.sessionForm.reset();
  elements.sessionSubmitButton.textContent = "Criar sessão";
  elements.sessionCancelEdit.classList.add("hidden");
}

function setSessionStatus(message) {
  elements.sessionFormStatus.textContent = message;
  elements.quickActionStatus.textContent = message;
}

function setFeedbackStatus(message) {
  elements.feedbackFormStatus.textContent = message;
  elements.quickActionStatus.textContent = message;
}

function toDateTimeLocalValue(value) {
  const date = new Date(value);
  const offsetMs = date.getTimezoneOffset() * 60000;
  return new Date(date.getTime() - offsetMs).toISOString().slice(0, 16);
}
