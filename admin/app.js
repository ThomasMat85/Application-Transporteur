const API_BASE = "https://application-transporteur.onrender.com";

const state = {
    user: null,
    tournees: [],
    camions: [],
    modeles: [],
    editingId: null,
    editingTerminee: false,
    etapes: []
};

const addressTimers = new WeakMap();

const $ = (selector) => document.querySelector(selector);
const $$ = (selector) => Array.from(document.querySelectorAll(selector));

document.addEventListener("DOMContentLoaded", () => {
    bindEvents();
    restoreSession();
    pingApi();
});

function bindEvents() {
    $("#loginForm").addEventListener("submit", login);
    $("#logoutBtn").addEventListener("click", logout);
    $("#newTourneeBtn").addEventListener("click", () => {
        resetTourneeForm();
        setTab("formulaire");
    });
    $("#resetFormBtn").addEventListener("click", resetTourneeForm);
    $("#addEtapeBtn").addEventListener("click", () => {
        state.etapes.push(createEtape());
        renumberEtapes();
        renderEtapes();
    });
    $("#tourneeForm").addEventListener("submit", saveTournee);
    $("#modeleForm").addEventListener("submit", saveModele);
    $("#adresseDepartRetourInput").addEventListener("input", (event) => {
        scheduleAddressSuggestions(event.target);
    });
    $("#etapesContainer").addEventListener("input", handleEtapeInput);
    $("#etapesContainer").addEventListener("change", handleEtapeChange);
    $("#etapesContainer").addEventListener("click", handleEtapeClick);
    $("#tourneesList").addEventListener("click", handleTourneeClick);
    $("#historiqueList").addEventListener("click", handleTourneeClick);
    $("#closeDialogBtn").addEventListener("click", () => $("#detailDialog").close());

    $$(".tab-button").forEach((button) => {
        button.addEventListener("click", () => setTab(button.dataset.tab));
    });
}

async function restoreSession() {
    const saved = sessionStorage.getItem("transcars-admin-user");

    if (!saved) {
        resetTourneeForm();
        return;
    }

    try {
        state.user = JSON.parse(saved);
        showDashboard();
        await loadData();
    } catch {
        sessionStorage.removeItem("transcars-admin-user");
        resetTourneeForm();
    }
}

async function pingApi() {
    try {
        await api("/api/camions-porteurs");
        $("#apiDot").className = "state-dot online";
        $("#apiText").textContent = "API en ligne";
    } catch {
        $("#apiDot").className = "state-dot offline";
        $("#apiText").textContent = "API indisponible";
    }
}

async function login(event) {
    event.preventDefault();
    $("#loginMessage").textContent = "";

    try {
        const result = await api("/api/utilisateurs/login", {
            method: "POST",
            body: JSON.stringify({
                nom: $("#loginName").value.trim(),
                motDePasse: $("#loginPassword").value
            })
        });

        if (!result.success || !result.utilisateur) {
            throw new Error(result.message || "Connexion refusee.");
        }

        if ((result.utilisateur.role || "").toLowerCase() !== "admin") {
            throw new Error("Ce compte n'est pas un compte admin.");
        }

        state.user = result.utilisateur;
        sessionStorage.setItem("transcars-admin-user", JSON.stringify(state.user));
        showDashboard();
        await loadData();
    } catch (error) {
        $("#loginMessage").textContent = error.message;
    }
}

function logout() {
    state.user = null;
    sessionStorage.removeItem("transcars-admin-user");
    $("#dashboardView").classList.add("hidden");
    $("#loginView").classList.remove("hidden");
    $("#logoutBtn").classList.add("hidden");
}

function showDashboard() {
    $("#loginView").classList.add("hidden");
    $("#dashboardView").classList.remove("hidden");
    $("#logoutBtn").classList.remove("hidden");
}

async function loadData() {
    const [tournees, camions, modeles] = await Promise.all([
        api("/api/tournees"),
        api("/api/camions-porteurs"),
        api("/api/modeles-vehicules")
    ]);

    state.tournees = tournees;
    state.camions = camions;
    state.modeles = modeles;

    renderCamions();
    renderTournees();
    renderHistorique();
    renderModeles();
    renderEtapes();
}

async function api(path, options = {}) {
    const response = await fetch(`${API_BASE}${path}`, {
        ...options,
        headers: {
            "Content-Type": "application/json",
            ...(options.headers || {})
        }
    });

    const text = await response.text();
    const data = parseResponse(text);

    if (!response.ok) {
        const message =
            typeof data === "string"
                ? data
                : data?.message || data?.title || `Erreur API ${response.status}`;
        throw new Error(message);
    }

    return data;
}

function parseResponse(text) {
    if (!text) {
        return null;
    }

    try {
        return JSON.parse(text);
    } catch {
        return text;
    }
}

function setTab(tab) {
    $$(".tab-button").forEach((button) => {
        button.classList.toggle("active", button.dataset.tab === tab);
    });

    $$(".tab-panel").forEach((panel) => {
        panel.classList.toggle("active", panel.id === `tab-${tab}`);
    });
}

function renderCamions() {
    const options = state.camions
        .map((camion) => {
            const label = `${camion.nom} - ${formatMeters(camion.longueurUtileCm * camion.nombreNiveaux)} utiles`;
            return `<option value="${camion.id}">${escapeHtml(label)}</option>`;
        })
        .join("");

    $("#camionSelect").innerHTML = options || "<option value=''>Aucun camion</option>";
    updateCapacityPreview();
}

function renderTournees() {
    if (state.tournees.length === 0) {
        $("#tourneesList").innerHTML = emptyState("Aucune tournee pour le moment.");
        return;
    }

    $("#tourneesList").innerHTML = state.tournees
        .map((tournee) => renderTourneeCard(tournee, false))
        .join("");
}

function renderHistorique() {
    const historique = state.tournees.filter((tournee) => tournee.terminee);

    if (historique.length === 0) {
        $("#historiqueList").innerHTML = emptyState("Aucune tournee terminee.");
        return;
    }

    $("#historiqueList").innerHTML = historique
        .map((tournee) => renderTourneeCard(tournee, true))
        .join("");
}

function renderTourneeCard(tournee, historyMode) {
    const vehicules = getVehicules(tournee);
    const nonPris = vehicules.filter((vehicule) => !vehicule.recupere);
    const statusClass = tournee.terminee ? "done" : "";
    const statusText = tournee.terminee ? "Terminee" : "En cours";
    const km = extractDistance(tournee.planOptimise);

    return `
        <article class="${historyMode ? "history-card" : "tournee-card"}">
            <div class="card-head">
                <div>
                    <h3>${escapeHtml(tournee.codeUnique)}</h3>
                    <div class="tournee-meta">
                        <span class="badge ${statusClass}">${statusText}</span>
                        <span>${vehicules.length} vehicule${vehicules.length > 1 ? "s" : ""}</span>
                        <span>${tournee.etapes.length} etape${tournee.etapes.length > 1 ? "s" : ""}</span>
                        ${km ? `<span>${escapeHtml(km)}</span>` : ""}
                    </div>
                </div>
                <div class="row-actions">
                    <button class="ghost-button" type="button" data-action="details" data-id="${tournee.id}">Detail</button>
                    <button class="secondary-button" type="button" data-action="edit" data-id="${tournee.id}">Modifier</button>
                    <button class="danger-button" type="button" data-action="delete" data-id="${tournee.id}">Supprimer</button>
                </div>
            </div>
            ${historyMode ? renderNonPris(nonPris) : renderTourneeStops(tournee)}
        </article>
    `;
}

function renderTourneeStops(tournee) {
    const stops = [...tournee.etapes]
        .sort((a, b) => a.ordre - b.ordre)
        .slice(0, 4)
        .map((etape) => `<span class="badge">${escapeHtml(etape.ordre)}. ${escapeHtml(etape.garage || etape.adresse)}</span>`)
        .join("");

    return `<div class="tournee-meta">${stops}</div>`;
}

function renderNonPris(nonPris) {
    if (nonPris.length === 0) {
        return `<div class="badge done">Tous les vehicules ont ete valides</div>`;
    }

    const items = nonPris
        .map((vehicule) => `${vehicule.marque} ${vehicule.modele} - ${vehicule.immatriculation}`.trim())
        .map((label) => `<span class="badge warn">${escapeHtml(label)}</span>`)
        .join("");

    return `<div class="tournee-meta">${items}</div>`;
}

function resetTourneeForm() {
    state.editingId = null;
    state.editingTerminee = false;
    state.etapes = [createEtape()];
    $("#formTitle").textContent = "Creer une tournee";
    $("#codeInput").value = "";
    $("#adresseDepartRetourInput").value = "";
    $("#codeInput").disabled = false;
    $("#camionSelect").disabled = false;
    renderEtapes();
    updateCapacityPreview();
}

function createEtape(source = {}) {
    return {
        uid: createId(),
        ordre: source.ordre || state.etapes.length + 1,
        garage: source.garage || "",
        adresse: source.adresse || "",
        vehicules: (source.vehicules || []).map(createVehicule)
    };
}

function createVehicule(source = {}) {
    const modele = source.modeleVehiculeId
        ? state.modeles.find((item) => item.id === Number(source.modeleVehiculeId))
        : null;

    return {
        uid: createId(),
        marque: source.marque || modele?.marque || "",
        modele: source.modele || modele?.modele || "",
        immatriculation: source.immatriculation || "",
        modeleVehiculeId: source.modeleVehiculeId || modele?.id || "",
        longueurCm: source.longueurCm || modele?.longueurCm || 0,
        largeurCm: source.largeurCm || modele?.largeurCm || 0,
        hauteurCm: source.hauteurCm || modele?.hauteurCm || 0,
        poidsKg: source.poidsKg || modele?.poidsKg || 0,
        adresseLivraison: source.adresseLivraison || "",
        clientLivraison: source.clientLivraison || "",
        recupere: Boolean(source.recupere),
        rechercheModele: source.rechercheModele || ""
    };
}

function renderEtapes() {
    $("#etapesContainer").innerHTML = state.etapes
        .map((etape, etapeIndex) => renderEtape(etape, etapeIndex))
        .join("");
    updateCapacityPreview();
}

function renderEtape(etape, etapeIndex) {
    const vehicules = etape.vehicules
        .map((vehicule, vehiculeIndex) => renderVehicule(vehicule, etapeIndex, vehiculeIndex))
        .join("");

    return `
        <article class="etape-card" data-etape-index="${etapeIndex}">
            <div class="card-head">
                <h3>Etape ${etapeIndex + 1}</h3>
                <div class="row-actions">
                    <button class="ghost-button" type="button" data-action="move-etape-up">Monter</button>
                    <button class="ghost-button" type="button" data-action="move-etape-down">Descendre</button>
                    <button class="danger-button" type="button" data-action="remove-etape">Supprimer</button>
                </div>
            </div>
            <div class="form-grid">
                <label>
                    Client / garage
                    <input value="${escapeAttr(etape.garage)}" data-field="garage" placeholder="Garage Peugeot">
                </label>
                <label>
                    Adresse de chargement
                    <input value="${escapeAttr(etape.adresse)}" data-field="adresse" data-autocomplete="address" list="adresse-etape-${etape.uid}" placeholder="10 rue...">
                    <datalist id="adresse-etape-${etape.uid}"></datalist>
                </label>
            </div>
            <div class="vehicle-list">${vehicules}</div>
            <div class="form-actions">
                <button class="secondary-button" type="button" data-action="add-vehicule">Ajouter vehicule</button>
            </div>
        </article>
    `;
}

function renderVehicule(vehicule, etapeIndex, vehiculeIndex) {
    const livraisonListId = `adresse-livraison-${state.etapes[etapeIndex].uid}-${vehicule.uid}`;

    return `
        <article class="vehicule-card" data-vehicule-index="${vehiculeIndex}">
            <div class="card-head">
                <h4>Vehicule ${vehiculeIndex + 1}</h4>
                <button class="danger-button" type="button" data-action="remove-vehicule">Supprimer</button>
            </div>
            <div class="form-grid">
                <label>
                    Recherche modele
                    <input value="${escapeAttr(vehicule.rechercheModele)}" data-field="rechercheModele" placeholder="Peugeot 208">
                </label>
                <label>
                    Modele
                    <select data-field="modeleVehiculeId">
                        ${buildModelOptions(vehicule.modeleVehiculeId, vehicule.rechercheModele)}
                    </select>
                </label>
                <label>
                    Plaque
                    <input value="${escapeAttr(vehicule.immatriculation)}" data-field="immatriculation" placeholder="AA-123-AA">
                </label>
                <label>
                    Client livraison
                    <input value="${escapeAttr(vehicule.clientLivraison)}" data-field="clientLivraison" placeholder="Client">
                </label>
                <label>
                    Adresse livraison
                    <input value="${escapeAttr(vehicule.adresseLivraison)}" data-field="adresseLivraison" data-autocomplete="address" list="${livraisonListId}" placeholder="Adresse">
                    <datalist id="${livraisonListId}"></datalist>
                </label>
            </div>
            <div class="vehicule-specs">${renderVehicleSpecs(vehicule)}</div>
        </article>
    `;
}

function buildModelOptions(selectedId, filter = "") {
    const query = normalize(filter);
    const filtered = state.modeles.filter((modele) => {
        const label = `${modele.marque} ${modele.modele} ${modele.longueurCm} ${modele.largeurCm} ${modele.hauteurCm}`;
        return normalize(label).includes(query);
    });

    const groups = groupBy(filtered, (modele) => modele.marque || "Autres");
    let html = `<option value="">Choisir un modele</option>`;

    for (const marque of Object.keys(groups).sort((a, b) => a.localeCompare(b, "fr"))) {
        html += `<optgroup label="${escapeAttr(marque)}">`;
        html += groups[marque]
            .sort((a, b) => a.modele.localeCompare(b.modele, "fr"))
            .map((modele) => {
                const selected = Number(selectedId) === modele.id ? "selected" : "";
                const label = `${modele.modele} - ${modele.longueurCm}x${modele.largeurCm}x${modele.hauteurCm} cm`;
                return `<option value="${modele.id}" ${selected}>${escapeHtml(label)}</option>`;
            })
            .join("");
        html += "</optgroup>";
    }

    return html;
}

function handleEtapeInput(event) {
    const target = event.target;
    const field = target.dataset.field;

    if (!field) {
        return;
    }

    const { etape, vehicule, vehicleCard } = getFormTarget(target);

    if (vehicule) {
        vehicule[field] = field === "immatriculation"
            ? target.value.toUpperCase()
            : target.value;

        if (field === "rechercheModele") {
            const select = vehicleCard.querySelector('[data-field="modeleVehiculeId"]');
            select.innerHTML = buildModelOptions(vehicule.modeleVehiculeId, vehicule.rechercheModele);
        }
    } else if (etape) {
        etape[field] = target.value;
    }

    if (target.dataset.autocomplete === "address") {
        scheduleAddressSuggestions(target);
    }

    updateCapacityPreview();
}

function handleEtapeChange(event) {
    const target = event.target;

    if (target.dataset.field !== "modeleVehiculeId") {
        return;
    }

    const { vehicule } = getFormTarget(target);
    const modele = state.modeles.find((item) => item.id === Number(target.value));

    if (!vehicule || !modele) {
        return;
    }

    Object.assign(vehicule, {
        modeleVehiculeId: modele.id,
        marque: modele.marque,
        modele: modele.modele,
        longueurCm: modele.longueurCm,
        largeurCm: modele.largeurCm,
        hauteurCm: modele.hauteurCm,
        poidsKg: modele.poidsKg,
        rechercheModele: `${modele.marque} ${modele.modele}`
    });

    renderEtapes();
}

function handleEtapeClick(event) {
    const button = event.target.closest("button[data-action]");

    if (!button) {
        return;
    }

    const action = button.dataset.action;
    const etapeCard = button.closest(".etape-card");
    const etapeIndex = Number(etapeCard?.dataset.etapeIndex);
    const vehicleCard = button.closest(".vehicule-card");
    const vehiculeIndex = Number(vehicleCard?.dataset.vehiculeIndex);

    if (action === "add-vehicule") {
        state.etapes[etapeIndex].vehicules.push(createVehicule());
    }

    if (action === "remove-vehicule") {
        state.etapes[etapeIndex].vehicules.splice(vehiculeIndex, 1);
    }

    if (action === "remove-etape" && state.etapes.length > 1) {
        state.etapes.splice(etapeIndex, 1);
    }

    if (action === "move-etape-up" && etapeIndex > 0) {
        swap(state.etapes, etapeIndex, etapeIndex - 1);
    }

    if (action === "move-etape-down" && etapeIndex < state.etapes.length - 1) {
        swap(state.etapes, etapeIndex, etapeIndex + 1);
    }

    renumberEtapes();
    renderEtapes();
}

async function saveTournee(event) {
    event.preventDefault();

    try {
        const payloadEtapes = buildEtapesPayload();
        const camionPorteurId = Number($("#camionSelect").value) || null;

        if (payloadEtapes.length === 0) {
            throw new Error("Ajoute au moins une etape.");
        }

        const totalVehicules = payloadEtapes.flatMap((etape) => etape.vehicules).length;

        if (totalVehicules === 0) {
            throw new Error("Ajoute au moins un vehicule.");
        }

        let saved;

        if (state.editingId) {
            saved = await api(`/api/tournees/${state.editingId}/details`, {
                method: "PUT",
                body: JSON.stringify({
                    terminee: state.editingTerminee,
                    adresseDepartRetour: $("#adresseDepartRetourInput").value.trim(),
                    etapes: payloadEtapes
                })
            });
        } else {
            saved = await api("/api/tournees", {
                method: "POST",
                body: JSON.stringify({
                    codeUnique: $("#codeInput").value.trim() || null,
                    terminee: false,
                    adresseDepartRetour: $("#adresseDepartRetourInput").value.trim(),
                    camionPorteurId,
                    etapes: payloadEtapes
                })
            });
        }

        toast(`Tournee ${saved.codeUnique} enregistree.`);
        await loadData();
        resetTourneeForm();
        setTab("tournees");
        await showTourneeDetails(saved.id);
    } catch (error) {
        toast(error.message, true);
    }
}

function buildEtapesPayload() {
    return state.etapes.map((etape, etapeIndex) => ({
        ordre: etapeIndex + 1,
        garage: etape.garage.trim(),
        adresse: etape.adresse.trim(),
        vehicules: etape.vehicules.map((vehicule) => {
            if (!vehicule.modeleVehiculeId) {
                throw new Error("Chaque vehicule doit avoir un modele.");
            }

            return {
                marque: vehicule.marque,
                modele: vehicule.modele,
                immatriculation: vehicule.immatriculation.trim().toUpperCase(),
                modeleVehiculeId: Number(vehicule.modeleVehiculeId),
                longueurCm: Number(vehicule.longueurCm),
                largeurCm: Number(vehicule.largeurCm),
                hauteurCm: Number(vehicule.hauteurCm),
                poidsKg: Number(vehicule.poidsKg),
                adresseLivraison: vehicule.adresseLivraison.trim(),
                clientLivraison: vehicule.clientLivraison.trim(),
                recupere: Boolean(vehicule.recupere)
            };
        })
    }));
}

async function handleTourneeClick(event) {
    const button = event.target.closest("button[data-action]");

    if (!button) {
        return;
    }

    const id = Number(button.dataset.id);

    if (button.dataset.action === "details") {
        await showTourneeDetails(id);
    }

    if (button.dataset.action === "edit") {
        const tournee = await api(`/api/tournees/${id}`);
        loadTourneeIntoForm(tournee);
        setTab("formulaire");
    }

    if (button.dataset.action === "delete") {
        const tournee = state.tournees.find((item) => item.id === id);

        if (!confirm(`Supprimer la tournee ${tournee?.codeUnique || id} ?`)) {
            return;
        }

        await api(`/api/tournees/${id}`, { method: "DELETE" });
        toast("Tournee supprimee.");
        await loadData();
    }
}

function loadTourneeIntoForm(tournee) {
    state.editingId = tournee.id;
    state.editingTerminee = tournee.terminee;
    state.etapes = [...tournee.etapes]
        .sort((a, b) => a.ordre - b.ordre)
        .map(createEtape);

    $("#formTitle").textContent = `Modifier ${tournee.codeUnique}`;
    $("#codeInput").value = tournee.codeUnique;
    $("#adresseDepartRetourInput").value = tournee.adresseDepartRetour || "";
    $("#codeInput").disabled = true;
    $("#camionSelect").value = tournee.camionPorteurId || "";
    $("#camionSelect").disabled = true;
    renderEtapes();
}

async function showTourneeDetails(id) {
    const tournee = state.tournees.find((item) => item.id === id) || await api(`/api/tournees/${id}`);
    $("#detailTitle").textContent = `Tournee ${tournee.codeUnique}`;
    $("#detailBody").innerHTML = `<p class="small-note">Chargement des donnees...</p>`;
    $("#detailDialog").showModal();

    try {
        const [optimisation, chargement] = await Promise.all([
            api(`/api/tournees/${id}/optimisation`),
            api(`/api/tournees/${id}/chargement`).catch((error) => ({ erreur: error.message }))
        ]);

        $("#detailBody").innerHTML = renderDetailBody(tournee, optimisation, chargement);
    } catch (error) {
        $("#detailBody").innerHTML = `<p class="form-message">${escapeHtml(error.message)}</p>`;
    }
}

function renderDetailBody(tournee, optimisation, chargement) {
    const arrets = optimisation.arrets
        .map((arret) => `
            <article class="etape-card">
                <div class="card-head">
                    <h3>${arret.ordre}. ${escapeHtml(arret.type)} - ${escapeHtml(arret.nom || arret.adresse)}</h3>
                    <span class="badge">${Number(arret.distanceDepuisPrecedentKm || 0).toFixed(1)} km</span>
                </div>
                <p class="small-note">${escapeHtml(arret.adresse || "")}</p>
                <div class="tournee-meta">
                    ${arret.vehicules.map((vehicule) => `<span class="badge">${escapeHtml(vehicule)}</span>`).join("")}
                </div>
            </article>
        `)
        .join("");

    const chargementHtml = chargement.erreur
        ? `<p class="form-message">${escapeHtml(chargement.erreur)}</p>`
        : `
            <div class="metrics">
                <div class="metric"><strong>${chargement.vehiculesQuiRentrent}/${chargement.vehiculesTotal}</strong><span>Vehicules</span></div>
                <div class="metric"><strong>${formatMeters(chargement.longueurUtiliseeCm)}</strong><span>Longueur utilisee</span></div>
                <div class="metric"><strong>${formatMeters(chargement.longueurDisponibleCm)}</strong><span>Longueur camion</span></div>
                <div class="metric"><strong>${chargement.chargeUtiliseeKg} kg</strong><span>Charge</span></div>
            </div>
            ${chargement.raisons?.length ? `<pre>${escapeHtml(chargement.raisons.join("\n"))}</pre>` : ""}
        `;

    return `
        <div class="metrics">
            <div class="metric"><strong>${Number(optimisation.distanceApproxKm || 0).toFixed(1)} km</strong><span>Distance</span></div>
            <div class="metric"><strong>${tournee.etapes.length}</strong><span>Etapes</span></div>
            <div class="metric"><strong>${getVehicules(tournee).length}</strong><span>Vehicules</span></div>
            <div class="metric"><strong>${tournee.terminee ? "Oui" : "Non"}</strong><span>Terminee</span></div>
        </div>
        ${tournee.adresseDepartRetour ? `<p class="small-note">Depart / retour : ${escapeHtml(tournee.adresseDepartRetour)}</p>` : ""}
        ${chargementHtml}
        <section class="stack">${arrets}</section>
        <pre>${escapeHtml(tournee.planOptimise || "Plan optimise en cours de calcul.")}</pre>
    `;
}

async function saveModele(event) {
    event.preventDefault();

    try {
        await api("/api/modeles-vehicules", {
            method: "POST",
            body: JSON.stringify({
                marque: $("#modeleMarque").value.trim(),
                modele: $("#modeleNom").value.trim(),
                longueurCm: Number($("#modeleLongueur").value),
                largeurCm: Number($("#modeleLargeur").value),
                hauteurCm: Number($("#modeleHauteur").value),
                poidsKg: Number($("#modelePoids").value)
            })
        });

        $("#modeleForm").reset();
        toast("Modele ajoute.");
        state.modeles = await api("/api/modeles-vehicules");
        renderModeles();
        renderEtapes();
    } catch (error) {
        toast(error.message, true);
    }
}

function renderModeles() {
    if (state.modeles.length === 0) {
        $("#modelesList").innerHTML = emptyState("Aucun modele vehicule.");
        return;
    }

    const groups = groupBy(state.modeles, (modele) => modele.marque || "Autres");

    $("#modelesList").innerHTML = Object.keys(groups)
        .sort((a, b) => a.localeCompare(b, "fr"))
        .map((marque) => `
            <article class="modele-brand">
                <h3>${escapeHtml(marque)}</h3>
                <table class="modele-table">
                    <thead>
                        <tr>
                            <th>Modele</th>
                            <th>L</th>
                            <th>l</th>
                            <th>H</th>
                            <th>Kg</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${groups[marque]
                            .sort((a, b) => a.modele.localeCompare(b.modele, "fr"))
                            .map((modele) => `
                                <tr>
                                    <td>${escapeHtml(modele.modele)}</td>
                                    <td>${modele.longueurCm}</td>
                                    <td>${modele.largeurCm}</td>
                                    <td>${modele.hauteurCm}</td>
                                    <td>${modele.poidsKg}</td>
                                </tr>
                            `)
                            .join("")}
                    </tbody>
                </table>
            </article>
        `)
        .join("");
}

function updateCapacityPreview() {
    const camion = state.camions.find((item) => item.id === Number($("#camionSelect")?.value));
    const vehicules = state.etapes.flatMap((etape) => etape.vehicules);

    if (!camion) {
        $("#capacityPreview").innerHTML = "";
        return;
    }

    const longueurDisponible = camion.longueurUtileCm * Math.max(1, camion.nombreNiveaux);
    const longueurUtilisee = vehicules.reduce((total, vehicule) => total + Number(vehicule.longueurCm || 0), 0);
    const poidsUtilise = vehicules.reduce((total, vehicule) => total + Number(vehicule.poidsKg || 0), 0);
    const hauts = vehicules.filter((vehicule) => Number(vehicule.hauteurCm || 0) > camion.hauteurMaxAvantCm).length;
    const longueurOk = longueurUtilisee <= longueurDisponible;
    const poidsOk = poidsUtilise <= camion.chargeUtileKg;

    $("#capacityPreview").innerHTML = `
        <div class="metric">
            <strong>${vehicules.length}</strong>
            <span>Vehicules</span>
        </div>
        <div class="metric">
            <strong>${formatMeters(longueurUtilisee)} / ${formatMeters(longueurDisponible)}</strong>
            <span>${longueurOk ? "Longueur OK" : "Trop long"}</span>
        </div>
        <div class="metric">
            <strong>${poidsUtilise} / ${camion.chargeUtileKg} kg</strong>
            <span>${poidsOk ? "Charge OK" : "Trop lourd"}</span>
        </div>
        <div class="metric">
            <strong>${hauts}</strong>
            <span>Hauts arriere</span>
        </div>
    `;
}

function scheduleAddressSuggestions(input) {
    const previous = addressTimers.get(input);

    if (previous) {
        clearTimeout(previous);
    }

    const timer = setTimeout(async () => {
        const text = input.value.trim();

        if (text.length < 3 || !input.list) {
            return;
        }

        try {
            const suggestions = await api(`/api/adresses/autocomplete?texte=${encodeURIComponent(text)}`, {
                method: "GET"
            });
            input.list.innerHTML = suggestions
                .map((adresse) => `<option value="${escapeAttr(adresse)}"></option>`)
                .join("");
        } catch {
            input.list.innerHTML = "";
        }
    }, 280);

    addressTimers.set(input, timer);
}

function getFormTarget(target) {
    const etapeCard = target.closest(".etape-card");
    const vehicleCard = target.closest(".vehicule-card");
    const etapeIndex = Number(etapeCard?.dataset.etapeIndex);
    const vehiculeIndex = Number(vehicleCard?.dataset.vehiculeIndex);
    const etape = Number.isInteger(etapeIndex) ? state.etapes[etapeIndex] : null;
    const vehicule = etape && Number.isInteger(vehiculeIndex) ? etape.vehicules[vehiculeIndex] : null;

    return { etape, vehicule, etapeCard, vehicleCard };
}

function renderVehicleSpecs(vehicule) {
    if (!vehicule.modeleVehiculeId) {
        return "Modele a choisir dans la base.";
    }

    return `${escapeHtml(vehicule.marque)} ${escapeHtml(vehicule.modele)} - ${vehicule.longueurCm} x ${vehicule.largeurCm} x ${vehicule.hauteurCm} cm - ${vehicule.poidsKg} kg`;
}

function getVehicules(tournee) {
    return (tournee.etapes || []).flatMap((etape) => etape.vehicules || []);
}

function extractDistance(plan) {
    const match = (plan || "").match(/Distance approx\.\s*:\s*([^\n]+)/i);
    return match ? match[1].trim() : "";
}

function renumberEtapes() {
    state.etapes.forEach((etape, index) => {
        etape.ordre = index + 1;
    });
}

function createId() {
    if (window.crypto?.randomUUID) {
        return window.crypto.randomUUID();
    }

    return `id-${Date.now()}-${Math.random().toString(16).slice(2)}`;
}

function normalize(value) {
    return String(value || "")
        .normalize("NFD")
        .replace(/[\u0300-\u036f]/g, "")
        .toLowerCase();
}

function groupBy(items, getKey) {
    return items.reduce((groups, item) => {
        const key = getKey(item);
        groups[key] = groups[key] || [];
        groups[key].push(item);
        return groups;
    }, {});
}

function swap(items, first, second) {
    const value = items[first];
    items[first] = items[second];
    items[second] = value;
}

function formatMeters(cm) {
    return `${(Number(cm || 0) / 100).toFixed(2)} m`;
}

function emptyState(message) {
    return `<div class="tool-panel"><p class="small-note">${escapeHtml(message)}</p></div>`;
}

function toast(message, isError = false) {
    const toastElement = $("#toast");
    toastElement.textContent = message;
    toastElement.style.background = isError ? "var(--danger)" : "#111827";
    toastElement.classList.remove("hidden");

    setTimeout(() => {
        toastElement.classList.add("hidden");
    }, 3600);
}

function escapeHtml(value) {
    return String(value ?? "")
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#39;");
}

function escapeAttr(value) {
    return escapeHtml(value);
}
