// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

(function () {
    const config = window.pcBuilderConfig;
    if (!config || !Array.isArray(config.components)) {
        return;
    }

    const buildForm = document.getElementById("build-form");
    const checkButton = document.getElementById("check-compatibility-btn");
    const saveBuildButton = document.getElementById("save-build-btn");
    const refreshSavedBuildsButton = document.getElementById("refresh-saved-builds-btn");
    const exportJsonButton = document.getElementById("export-build-json");
    const exportTxtButton = document.getElementById("export-build-txt");
    const spinner = document.getElementById("validation-spinner");
    const alertsContainer = document.getElementById("compatibility-alerts");
    const compatibilityBadge = document.getElementById("summary-compat-badge");
    const wattagePill = document.getElementById("summary-wattage-pill");
    const powerHealthText = document.getElementById("summary-power-health-text");
    const buildCategory = document.getElementById("summary-build-category");
    const compatibilityPercentage = document.getElementById("summary-compatibility-percentage");
    const efficiencyScore = document.getElementById("summary-efficiency-score");
    const thermalScore = document.getElementById("summary-thermal-score");
    const psuHealthScore = document.getElementById("summary-psu-health-score");
    const cpuTemp = document.getElementById("summary-cpu-temp");
    const gpuTemp = document.getElementById("summary-gpu-temp");
    const fps1080p = document.getElementById("summary-fps-1080p");
    const fps1440p = document.getElementById("summary-fps-1440p");
    const fps4k = document.getElementById("summary-fps-4k");
    const fpsRayTracing = document.getElementById("summary-fps-raytracing");
    const fpsCompetitive = document.getElementById("summary-fps-competitive");
    const fpsAaa = document.getElementById("summary-fps-aaa");
    const badgesContainer = document.getElementById("summary-build-badges");
    const cpuBottleneck = document.getElementById("summary-cpu-bottleneck");
    const gpuBottleneck = document.getElementById("summary-gpu-bottleneck");
    const recommendationsList = document.getElementById("recommendations-list");
    const savedBuildsList = document.getElementById("saved-builds-list");
    const saveToastElement = document.getElementById("save-toast");
    const saveToastMessage = document.getElementById("save-toast-message");
    const selects = Array.from(document.querySelectorAll(".component-select"));
    const quickCooler = document.getElementById("quick-cooler");

    if (!buildForm || !checkButton || !saveBuildButton || !exportJsonButton || !exportTxtButton || !spinner || !alertsContainer || !compatibilityBadge || !wattagePill || !powerHealthText || !buildCategory || !compatibilityPercentage || !efficiencyScore || !thermalScore || !psuHealthScore || !cpuTemp || !gpuTemp || !fps1080p || !fps1440p || !fps4k || !fpsRayTracing || !fpsCompetitive || !fpsAaa || !badgesContainer || !cpuBottleneck || !gpuBottleneck || !recommendationsList || !savedBuildsList || !saveToastElement || !saveToastMessage || !quickCooler || selects.length === 0) {
        return;
    }

    const componentsById = new Map(config.components.map(component => [String(component.id), component]));
    let savedBuilds = Array.isArray(config.savedBuilds) ? [...config.savedBuilds] : [];
    const formatter = new Intl.NumberFormat(undefined, {
        style: "currency",
        currency: "USD",
        minimumFractionDigits: 2
    });

    function escapeHtml(value) {
        if (value === null || value === undefined) {
            return "";
        }

        return String(value)
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#39;");
    }

    function savedBuildDisplayName(build) {
        const raw = build?.name;
        if (raw != null && String(raw).trim().length > 0) {
            return escapeHtml(String(raw).trim());
        }

        const id = build?.id != null ? String(build.id).replace(/-/g, "") : "";
        const shortId = id.length >= 8 ? id.slice(0, 8) : id || "····";
        return `Untitled · ${escapeHtml(shortId)}`;
    }

    buildForm.addEventListener("submit", event => {
        event.preventDefault();
    });

    createIncompatibleWarningIndicators();
    selects.forEach(select => {
        select.addEventListener("change", async () => {
            updateSummary();
            await refreshCompatibleOptions();
        });
    });

    checkButton.addEventListener("click", async () => {
        await validateBuild();
    });

    saveBuildButton.addEventListener("click", async () => {
        await saveBuild();
    });

    exportJsonButton.addEventListener("click", async () => {
        await exportBuild("json");
    });

    exportTxtButton.addEventListener("click", async () => {
        await exportBuild("txt");
    });

    if (refreshSavedBuildsButton) {
        refreshSavedBuildsButton.addEventListener("click", async () => {
            await loadSavedBuilds();
        });
    }

    updateSummary();
    renderSavedBuildCards(savedBuilds);
    wireLoadBuildButtons();
    wireCompareBuildButtons();
    void refreshCompatibleOptions();

    if (config.isAuthenticated !== true) {
        saveBuildButton.classList.add("d-none");
    }

    async function exportBuild(format) {
        if (!config.exportBuildUrl) {
            showToast("Export endpoint is not configured.");
            return;
        }

        const url = new URL(config.exportBuildUrl, window.location.origin);
        const query = new URLSearchParams({
            cpuId: getSelectedId("CpuId"),
            motherboardId: getSelectedId("MotherboardId"),
            ramId: getSelectedId("RamId"),
            gpuId: getSelectedId("GpuId"),
            psuId: getSelectedId("PsuId"),
            caseId: getSelectedId("CaseId"),
            coolerId: getSelectedId("CoolerId"),
            format: String(format ?? "json")
        });
        url.search = query.toString();

        try {
            const response = await fetch(url, { method: "GET" });
            if (!response.ok) {
                showToast("Export failed. Please try again.");
                return;
            }

            const blob = await response.blob();
            const filename = format === "txt" ? "pc-build.txt" : "pc-build.json";
            downloadBlob(blob, filename);
        } catch (error) {
            console.error(error);
            showToast("Unexpected error while exporting build.");
        }
    }

    function downloadBlob(blob, filename) {
        const objectUrl = URL.createObjectURL(blob);
        const link = document.createElement("a");
        link.href = objectUrl;
        link.download = filename;
        document.body.appendChild(link);
        link.click();
        link.remove();
        URL.revokeObjectURL(objectUrl);
    }

    function getComponent(selectId) {
        const select = document.getElementById(selectId);
        if (!select || !select.value) {
            return null;
        }

        return componentsById.get(select.value) ?? null;
    }

    function getSelectedId(selectId) {
        const select = document.getElementById(selectId);
        if (!select || !select.value) {
            return "";
        }

        return String(select.value);
    }

    function setText(id, value) {
        const element = document.getElementById(id);
        if (element) {
            element.textContent = value;
        }
    }

    function createIncompatibleWarningIndicators() {
        selects.forEach(select => {
            if (select.nextElementSibling && select.nextElementSibling.classList.contains("incompatible-warning-icon")) {
                return;
            }

            const indicator = document.createElement("span");
            indicator.className = "incompatible-warning-icon d-none";
            indicator.textContent = "⚠";
            indicator.setAttribute("title", "Selected component is currently incompatible.");
            select.insertAdjacentElement("afterend", indicator);
        });
    }

    function updateSummary() {
        const cpu = getComponent("CpuId");
        const motherboard = getComponent("MotherboardId");
        const ram = getComponent("RamId");
        const gpu = getComponent("GpuId");
        const psu = getComponent("PsuId");
        const pcCase = getComponent("CaseId");
        const cooler = getComponent("CoolerId");

        setText("summary-cpu", cpu?.name ?? "-");
        setText("summary-motherboard", motherboard?.name ?? "-");
        setText("summary-ram", ram?.name ?? "-");
        setText("summary-gpu", gpu?.name ?? "-");
        setText("summary-psu", psu?.name ?? "-");
        setText("summary-case", pcCase?.name ?? "-");
        setText("summary-cooler", cooler?.name ?? "-");

        const cpuGen = cpu?.generation ? `${cpu.generation} · ` : "";
        const cpuTier = cpu?.performanceTier ? ` · ${cpu.performanceTier}` : "";
        setText("quick-cpu", cpu ? `${cpuGen}${cpu.cores}C/${cpu.threads}T · ${cpu.baseClockGhz}GHz · ${cpu.socket}${cpuTier}` : "-");
        const mbFf = motherboard?.formFactor ?? "—";
        setText("quick-motherboard", motherboard ? `${motherboard.chipset} · ${mbFf} · ${motherboard.socket}` : "-");
        const ramTier = ram?.performanceTier ? ` · ${ram.performanceTier}` : "";
        setText("quick-ram", ram ? `${ram.capacityGb}GB · ${ram.frequencyMhz}MHz · ${ram.ramType}${ramTier}` : "-");
        const gpuLen = gpu?.lengthMm != null ? ` · ${gpu.lengthMm}mm` : "";
        const gpuTier = gpu?.performanceTier ? ` · ${gpu.performanceTier}` : "";
        setText("quick-gpu", gpu ? `${gpu.vramGb}GB VRAM · ${gpu.tdpWatts}W TDP${gpuLen}${gpuTier}` : "-");
        setText("quick-psu", psu ? `${psu.wattage}W · ${psu.efficiencyRating}` : "-");
        const caseMaxGpu = pcCase?.maxGpuLengthMm != null ? ` · Max GPU ${pcCase.maxGpuLengthMm}mm` : "";
        const caseAirflow = pcCase?.airflowRating != null && Number(pcCase.airflowRating) > 0 ? ` · Airflow ${pcCase.airflowRating}/100` : "";
        const caseFans = pcCase?.includedFans != null && Number(pcCase.includedFans) > 0 ? ` · Fans ${pcCase.includedFans}` : "";
        setText("quick-case", pcCase ? `${pcCase.supportedMotherboardSizes?.join(", ") ?? pcCase.supportedFormFactors?.join(", ") ?? "N/A"}${caseMaxGpu}${caseFans}${caseAirflow}` : "-");

        const coolerSockets = Array.isArray(cooler?.supportedSockets) ? cooler.supportedSockets.join(", ") : "N/A";
        const coolerNoise = cooler?.noiseLevelDb != null && Number(cooler.noiseLevelDb) > 0 ? ` · ${Number(cooler.noiseLevelDb).toFixed(1)}dB` : "";
        setText("quick-cooler", cooler ? `${cooler.coolingCapacityWatts ?? 0}W · ${coolerSockets}${coolerNoise}` : "-");

        const totalPrice =
            (cpu?.price ?? 0) +
            (motherboard?.price ?? 0) +
            (ram?.price ?? 0) +
            (gpu?.price ?? 0) +
            (psu?.price ?? 0) +
            (pcCase?.price ?? 0) +
            (cooler?.price ?? 0);

        const cpuWatts = cpu?.tdpWatts ?? 0;
        const gpuWatts = gpu?.tdpWatts ?? 0;
        const ramCapacityGb = ram?.capacityGb ?? 0;
        const ramReserve = Math.ceil((ramCapacityGb / 16) * 5);
        const estimatedWattage = cpuWatts === 0 && gpuWatts === 0 && ramCapacityGb === 0
            ? 0
            : cpuWatts + gpuWatts + 100 + ramReserve;

        setText("summary-total-price", formatter.format(totalPrice));
        setText("summary-estimated-wattage", `${estimatedWattage} W`);
        updateWattageIndicator(estimatedWattage, psu?.wattage ?? 0, gpu?.recommendedPsuWattage ?? 0);
        resetIntelligenceDisplay();
    }

    async function validateBuild() {
        return await validateBuildCore(true);
    }

    async function refreshCompatibleOptions() {
        const url = new URL(config.compatibleOptionsUrl, window.location.origin);
        const query = new URLSearchParams({
            cpuId: getSelectedId("CpuId"),
            motherboardId: getSelectedId("MotherboardId"),
            ramId: getSelectedId("RamId"),
            gpuId: getSelectedId("GpuId"),
            psuId: getSelectedId("PsuId")
        });
        url.search = query.toString();

        try {
            const response = await fetch(url, { method: "GET" });
            if (!response.ok) {
                return;
            }

            const data = await response.json();
            const compatibleMotherboardIds = new Set((data.compatibleMotherboardIds ?? []).map(String));
            const compatibleRamIds = new Set((data.compatibleRamIds ?? []).map(String));
            const recommendedPsuIds = new Set((data.recommendedPsuIds ?? []).map(String));

            applyOptionState("MotherboardId", compatibleMotherboardIds, null);
            applyOptionState("RamId", compatibleRamIds, null);
            applyOptionState("PsuId", recommendedPsuIds, recommendedPsuIds);
            updateSelectedIncompatibleWarnings();
        } catch (error) {
            console.error(error);
        }
    }

    function applyOptionState(selectId, compatibleIds, recommendedIds) {
        const select = document.getElementById(selectId);
        if (!select) {
            return;
        }

        Array.from(select.options).forEach(option => {
            if (!option.value) {
                option.disabled = false;
                option.classList.remove("compatible-option", "incompatible-option", "recommended-option");
                return;
            }

            const optionId = String(option.value);
            const isCompatible = compatibleIds ? compatibleIds.has(optionId) : true;
            const isRecommended = recommendedIds ? recommendedIds.has(optionId) : false;

            option.classList.remove("compatible-option", "incompatible-option", "recommended-option");
            if (isCompatible) {
                option.classList.add("compatible-option");
            } else {
                option.classList.add("incompatible-option");
            }

            if (isRecommended) {
                option.classList.add("recommended-option");
            }

            option.disabled = !isCompatible;
        });

        const selectedOption = select.options[select.selectedIndex];
        const selectedIncompatible = selectedOption && selectedOption.value && selectedOption.disabled;
        select.classList.toggle("selected-incompatible", Boolean(selectedIncompatible));
    }

    function updateSelectedIncompatibleWarnings() {
        selects.forEach(select => {
            const selectedOption = select.options[select.selectedIndex];
            const isIncompatible = Boolean(selectedOption && selectedOption.value && selectedOption.disabled);
            const warningIcon = select.nextElementSibling;
            if (!warningIcon || !warningIcon.classList.contains("incompatible-warning-icon")) {
                return;
            }

            warningIcon.classList.toggle("d-none", !isIncompatible);
        });
    }

    async function validateBuildCore(showSpinner) {
        if (showSpinner) {
            spinner.classList.remove("d-none");
            checkButton.disabled = true;
        }

        try {
            const formData = new FormData(buildForm);
            const response = await fetch(config.validateUrl, {
                method: "POST",
                body: formData
            });

            if (!response.ok) {
                renderAlerts(["Validation request failed. Please try again."], [], false);
                return;
            }

            const data = await response.json();
            renderAlerts(data.errors ?? [], data.warnings ?? [], data.isValid === true);

            if (data.selectedNames) {
                setText("summary-cpu", data.selectedNames.cpu ?? "-");
                setText("summary-motherboard", data.selectedNames.motherboard ?? "-");
                setText("summary-ram", data.selectedNames.ram ?? "-");
                setText("summary-gpu", data.selectedNames.gpu ?? "-");
                setText("summary-psu", data.selectedNames.psu ?? "-");
                setText("summary-case", data.selectedNames.case ?? "-");
                setText("summary-cooler", data.selectedNames.cooler ?? "-");
            }

            const totalPrice = Number(data.totalPrice ?? 0);
            const estimatedWattage = Number(data.estimatedWattage ?? 0);
            setText("summary-total-price", formatter.format(totalPrice));
            setText("summary-estimated-wattage", `${estimatedWattage} W`);

            const psu = getComponent("PsuId");
            const gpu = getComponent("GpuId");
            updateWattageIndicator(estimatedWattage, psu?.wattage ?? 0, gpu?.recommendedPsuWattage ?? 0);
            updateIntelligenceDisplay(data);
            return data;
        } catch (error) {
            console.error(error);
            renderAlerts(["Unexpected error while validating compatibility."], [], false);
            return null;
        } finally {
            if (showSpinner) {
                spinner.classList.add("d-none");
                checkButton.disabled = false;
            }
        }
    }

    function renderAlerts(errors, warnings, isValid) {
        alertsContainer.innerHTML = "";

        if (errors.length > 0) {
            alertsContainer.appendChild(buildAlert("danger", "Compatibility issues found", errors));
            setCompatibilityBadge("Incompatible", "text-bg-danger");
        } else if (isValid) {
            alertsContainer.appendChild(buildAlert("success", "Compatibility check passed", ["Selected components are compatible."]));
            setCompatibilityBadge("Compatible", "text-bg-success");
        } else {
            setCompatibilityBadge("Not validated", "text-bg-secondary");
        }

        if (warnings.length > 0) {
            alertsContainer.appendChild(buildAlert("warning", "Warnings", warnings));
            if (errors.length === 0) {
                setCompatibilityBadge("Compatible with warnings", "text-bg-warning");
            }
        }
    }

    function buildAlert(type, title, messages) {
        const wrapper = document.createElement("div");
        wrapper.className = `alert alert-${type} alert-animate`;
        wrapper.setAttribute("role", "alert");

        const heading = document.createElement("h2");
        heading.className = "h5";
        heading.textContent = title;
        wrapper.appendChild(heading);

        const list = document.createElement("ul");
        list.className = "mb-0";

        messages.forEach(message => {
            const item = document.createElement("li");
            item.textContent = message;
            list.appendChild(item);
        });

        wrapper.appendChild(list);
        return wrapper;
    }

    function setCompatibilityBadge(label, badgeClass) {
        compatibilityBadge.className = `badge rounded-pill px-3 py-2 ${badgeClass}`;
        compatibilityBadge.textContent = label;
    }

    function resetIntelligenceDisplay() {
        buildCategory.textContent = "Uncategorized";
        compatibilityPercentage.textContent = "0%";
        efficiencyScore.textContent = "0 / 100";
        thermalScore.textContent = "0 / 100";
        psuHealthScore.textContent = "0 / 100";
        cpuTemp.textContent = "0C";
        gpuTemp.textContent = "0C";
        fps1080p.textContent = "N/A";
        fps1440p.textContent = "N/A";
        fps4k.textContent = "N/A";
        fpsRayTracing.textContent = "N/A";
        fpsCompetitive.textContent = "N/A";
        fpsAaa.textContent = "N/A";
        cpuBottleneck.textContent = "0%";
        gpuBottleneck.textContent = "0%";
        badgesContainer.innerHTML = '<span class="badge text-bg-secondary">No badges yet</span>';
        recommendationsList.innerHTML = '<div class="col-12"><div class="alert alert-info mb-0">Run compatibility check to see build recommendations.</div></div>';
        compatibilityPercentage.className = "";
        efficiencyScore.className = "";
        thermalScore.className = "";
        psuHealthScore.className = "";
        cpuTemp.className = "";
        gpuTemp.className = "";
        cpuBottleneck.className = "";
        gpuBottleneck.className = "";
    }

    function updateIntelligenceDisplay(data) {
        const category = data.buildCategory ?? "Uncategorized";
        const compPercent = Number(data.compatibilityPercentage ?? 0);
        const effScore = Number(data.efficiencyScore ?? 0);
        const thermScore = Number(data.thermalHealthScore ?? 0);
        const psuScore = Number(data.psuHealthScore ?? 0);
        const cpu = Number(data.estimatedCpuTemperatureCelsius ?? 0);
        const gpu = Number(data.estimatedGpuTemperatureCelsius ?? 0);
        const estimatedFps1080 = Number(data.estimatedFps1080p ?? 0);
        const estimatedFps1440 = Number(data.estimatedFps1440p ?? 0);
        const estimatedFps4k = Number(data.estimatedFps4k ?? 0);
        const estimatedFpsRt = Number(data.estimatedRayTracingFps ?? 0);
        const estimatedFpsCompetitive = Number(data.estimatedCompetitiveFps ?? 0);
        const estimatedFpsAaa = Number(data.estimatedAaaFps ?? 0);
        const cpuBottle = Number(data.cpuBottleneckPercentage ?? 0);
        const gpuBottle = Number(data.gpuBottleneckPercentage ?? 0);

        buildCategory.textContent = category;
        compatibilityPercentage.textContent = `${compPercent}%`;
        efficiencyScore.textContent = `${effScore} / 100`;
        thermalScore.textContent = `${thermScore} / 100`;
        psuHealthScore.textContent = `${psuScore} / 100`;
        cpuTemp.textContent = `${cpu}C`;
        gpuTemp.textContent = `${gpu}C`;

        const isGamingCategory = String(category).toLowerCase() === "gaming";
        fps1080p.textContent = isGamingCategory && estimatedFps1080 > 0 ? `${estimatedFps1080} FPS` : "N/A";
        fps1440p.textContent = isGamingCategory && estimatedFps1440 > 0 ? `${estimatedFps1440} FPS` : "N/A";
        fps4k.textContent = isGamingCategory && estimatedFps4k > 0 ? `${estimatedFps4k} FPS` : "N/A";
        fpsRayTracing.textContent = isGamingCategory && estimatedFpsRt > 0 ? `${estimatedFpsRt} FPS` : "N/A";
        fpsCompetitive.textContent = isGamingCategory && estimatedFpsCompetitive > 0 ? `${estimatedFpsCompetitive} FPS` : "N/A";
        fpsAaa.textContent = isGamingCategory && estimatedFpsAaa > 0 ? `${estimatedFpsAaa} FPS` : "N/A";
        cpuBottleneck.textContent = `${cpuBottle}%`;
        gpuBottleneck.textContent = `${gpuBottle}%`;

        setHealthClass(compatibilityPercentage, compPercent, [70, 90]);
        setHealthClass(efficiencyScore, effScore, [60, 80]);
        setHealthClass(thermalScore, thermScore, [60, 80]);
        setHealthClass(psuHealthScore, psuScore, [50, 75]);

        setTemperatureClass(cpuTemp, cpu);
        setTemperatureClass(gpuTemp, gpu);
        setHealthClass(cpuBottleneck, Math.max(0, 100 - cpuBottle), [65, 85]);
        setHealthClass(gpuBottleneck, Math.max(0, 100 - gpuBottle), [65, 85]);
        renderBadges(Array.isArray(data.buildBadges) ? data.buildBadges : []);
        renderRecommendations(Array.isArray(data.recommendations) ? data.recommendations : []);
    }

    function renderBadges(badges) {
        badgesContainer.innerHTML = "";
        if (badges.length === 0) {
            badgesContainer.innerHTML = '<span class="badge text-bg-secondary">No badges earned</span>';
            return;
        }

        badges.forEach(badge => {
            const element = document.createElement("span");
            element.className = `badge ${mapBadgeStyle(badge)}`;
            element.textContent = badge;
            badgesContainer.appendChild(element);
        });
    }

    function mapBadgeStyle(badge) {
        switch (badge) {
            case "Excellent Build":
                return "text-bg-success";
            case "Balanced Build":
                return "text-bg-primary";
            case "High Performance":
                return "text-bg-info";
            case "Power Hungry":
                return "text-bg-danger";
            case "Budget Friendly":
                return "text-bg-warning";
            default:
                return "text-bg-secondary";
        }
    }

    function setHealthClass(element, value, thresholds) {
        element.className = "fw-semibold";
        if (value < thresholds[0]) {
            element.classList.add("text-danger");
            return;
        }

        if (value < thresholds[1]) {
            element.classList.add("text-warning");
            return;
        }

        element.classList.add("text-success");
    }

    function setTemperatureClass(element, value) {
        element.className = "fw-semibold";
        if (value === 0) {
            element.classList.add("text-secondary");
            return;
        }

        if (value >= 85) {
            element.classList.add("text-danger");
            return;
        }

        if (value >= 72) {
            element.classList.add("text-warning");
            return;
        }

        element.classList.add("text-success");
    }

    function renderRecommendations(recommendations) {
        recommendationsList.innerHTML = "";
        if (recommendations.length === 0) {
            recommendationsList.innerHTML = '<div class="col-12"><div class="alert alert-info mb-0">No recommendations available.</div></div>';
            return;
        }

        recommendations.forEach(text => {
            const parsed = parseRecommendation(text);
            const col = document.createElement("div");
            col.className = "col-12 col-md-6";

            const card = document.createElement("div");
            card.className = `card h-100 border-${parsed.color}`;

            const body = document.createElement("div");
            body.className = "card-body py-2";

            const title = document.createElement("div");
            title.className = `fw-semibold text-${parsed.color} mb-1`;
            title.textContent = `${parsed.icon} ${parsed.level} priority`;

            const message = document.createElement("div");
            message.className = "small";
            message.textContent = parsed.message;

            body.appendChild(title);
            body.appendChild(message);
            card.appendChild(body);
            col.appendChild(card);
            recommendationsList.appendChild(col);
        });
    }

    function parseRecommendation(rawText) {
        const normalized = String(rawText ?? "");
        if (normalized.startsWith("[high]")) {
            return {
                level: "High",
                color: "danger",
                icon: "⚠",
                message: normalized.replace("[high]", "").trim()
            };
        }

        if (normalized.startsWith("[medium]")) {
            return {
                level: "Medium",
                color: "warning",
                icon: "⚡",
                message: normalized.replace("[medium]", "").trim()
            };
        }

        return {
            level: "Low",
            color: "info",
            icon: "ℹ",
            message: normalized.replace("[low]", "").trim()
        };
    }

    async function saveBuild() {
        saveBuildButton.disabled = true;
        try {
            const validation = await validateBuildCore(false);
            if (!validation) {
                showToast("Failed to save build.");
                return;
            }

            const formData = new FormData(buildForm);
            const response = await fetch(config.saveBuildUrl, {
                method: "POST",
                body: formData
            });

            if (response.status === 401 || response.status === 403) {
                showToast("Please login to save builds.");
                return;
            }

            if (!response.ok) {
                showToast("Save request failed.");
                return;
            }

            const data = await response.json();
            if (!data.saved || !data.build) {
                showToast("Build could not be saved.");
                return;
            }

            savedBuilds = [data.build, ...savedBuilds];
            renderSavedBuildCards(savedBuilds);
            wireLoadBuildButtons();
            wireCompareBuildButtons();
            const nameInput = document.getElementById("save-build-name");
            if (nameInput) {
                nameInput.value = "";
            }

            const pubInput = document.getElementById("save-build-public");
            if (pubInput) {
                pubInput.checked = false;
            }

            showToast(data.message ?? "Build saved successfully.");
        } catch (error) {
            console.error(error);
            showToast("Unexpected error while saving build.");
        } finally {
            saveBuildButton.disabled = false;
        }
    }

    async function loadSavedBuilds() {
        try {
            const response = await fetch(config.savedBuildsUrl, { method: "GET" });
            if (!response.ok) {
                return;
            }

            const data = await response.json();
            savedBuilds = Array.isArray(data) ? data : [];
            renderSavedBuildCards(savedBuilds);
            wireLoadBuildButtons();
            wireCompareBuildButtons();
        } catch (error) {
            console.error(error);
        }
    }

    function renderSavedBuildCards(builds) {
        savedBuildsList.innerHTML = "";
        if (!Array.isArray(builds) || builds.length === 0) {
            savedBuildsList.innerHTML = '<div class="col-12"><div class="alert alert-light border mb-0">No saved builds yet.</div></div>';
            return;
        }

        builds.forEach(build => {
            const card = document.createElement("div");
            card.className = "col-12 col-md-6 col-xl-4";

            const created = new Date(build.createdAtUtc);
            const createdLabel = Number.isNaN(created.getTime()) ? "-" : created.toLocaleString();
            const titleHtml = savedBuildDisplayName(build);
            const isPublic = build.isPublic === true;
            const visibilityBadge = isPublic
                ? `<span class="badge text-bg-success flex-shrink-0">Public</span>`
                : `<span class="badge text-bg-secondary flex-shrink-0">Private</span>`;
            const fps = Number(build.estimatedFps1080p ?? 0);
            const fpsBadge =
                fps > 0
                    ? `<span class="badge text-bg-info text-dark">${fps} FPS · 1080p</span>`
                    : "";

            card.innerHTML = `
                <div class="card border h-100 saved-build-card">
                    <div class="card-body">
                        <div class="d-flex justify-content-between align-items-start gap-2 mb-2">
                            <h3 class="h6 fw-semibold mb-0 flex-grow-1">${titleHtml}</h3>
                            ${visibilityBadge}
                        </div>
                        <div class="d-flex flex-wrap gap-1 mb-2">
                            <span class="badge text-bg-primary">${escapeHtml(build.buildCategory ?? "Uncategorized")}</span>
                            <span class="badge text-bg-dark">${Number(build.compatibilityPercentage ?? 0)}%</span>
                            ${fpsBadge}
                        </div>
                        <div class="small text-secondary mb-1">Total price: <strong>${formatter.format(Number(build.totalPrice ?? 0))}</strong></div>
                        <div class="small text-secondary mb-1">Estimated wattage: <strong>${Number(build.estimatedWattage ?? 0)} W</strong></div>
                        <div class="small text-secondary mb-3">Created: <strong>${createdLabel}</strong></div>
                        <div class="d-grid gap-2">
                            <button
                                type="button"
                                class="btn btn-outline-primary btn-sm w-100 load-build-btn"
                                data-cpu-id="${build.cpuId ?? ""}"
                                data-motherboard-id="${build.motherboardId ?? ""}"
                                data-ram-id="${build.ramId ?? ""}"
                                data-gpu-id="${build.gpuId ?? ""}"
                                data-psu-id="${build.psuId ?? ""}"
                                data-case-id="${build.caseId ?? ""}"
                                data-cooler-id="${build.coolerId ?? ""}">
                                Load build
                            </button>
                            <button
                                type="button"
                                class="btn btn-outline-dark btn-sm w-100 compare-build-btn"
                                data-cpu-id="${build.cpuId ?? ""}"
                                data-motherboard-id="${build.motherboardId ?? ""}"
                                data-ram-id="${build.ramId ?? ""}"
                                data-gpu-id="${build.gpuId ?? ""}"
                                data-psu-id="${build.psuId ?? ""}"
                                data-case-id="${build.caseId ?? ""}"
                                data-cooler-id="${build.coolerId ?? ""}">
                                Compare with current build
                            </button>
                        </div>
                    </div>
                </div>`;
            savedBuildsList.appendChild(card);
        });
    }

    function wireLoadBuildButtons() {
        const loadButtons = Array.from(document.querySelectorAll(".load-build-btn"));
        loadButtons.forEach(button => {
            button.addEventListener("click", async () => {
                setSelectValue("CpuId", button.getAttribute("data-cpu-id"));
                setSelectValue("MotherboardId", button.getAttribute("data-motherboard-id"));
                setSelectValue("RamId", button.getAttribute("data-ram-id"));
                setSelectValue("GpuId", button.getAttribute("data-gpu-id"));
                setSelectValue("PsuId", button.getAttribute("data-psu-id"));
                setSelectValue("CaseId", button.getAttribute("data-case-id"));
                setSelectValue("CoolerId", button.getAttribute("data-cooler-id"));
                updateSummary();
                await refreshCompatibleOptions();
                await validateBuild();
            });
        });
    }

    function wireCompareBuildButtons() {
        const compareButtons = Array.from(document.querySelectorAll(".compare-build-btn"));
        compareButtons.forEach(button => {
            button.addEventListener("click", () => {
                const query = new URLSearchParams({
                    currentCpuId: getSelectedId("CpuId"),
                    currentMotherboardId: getSelectedId("MotherboardId"),
                    currentRamId: getSelectedId("RamId"),
                    currentGpuId: getSelectedId("GpuId"),
                    currentPsuId: getSelectedId("PsuId"),
                    currentCaseId: getSelectedId("CaseId"),
                    currentCoolerId: getSelectedId("CoolerId"),
                    compareCpuId: button.getAttribute("data-cpu-id") ?? "",
                    compareMotherboardId: button.getAttribute("data-motherboard-id") ?? "",
                    compareRamId: button.getAttribute("data-ram-id") ?? "",
                    compareGpuId: button.getAttribute("data-gpu-id") ?? "",
                    comparePsuId: button.getAttribute("data-psu-id") ?? "",
                    compareCaseId: button.getAttribute("data-case-id") ?? "",
                    compareCoolerId: button.getAttribute("data-cooler-id") ?? ""
                });
                window.location.href = `${config.compareUrl}?${query.toString()}`;
            });
        });
    }

    function setSelectValue(selectId, rawValue) {
        const element = document.getElementById(selectId);
        if (!element) {
            return;
        }

        const value = rawValue == null ? "" : String(rawValue);
        element.value = value;
    }

    function showToast(message) {
        saveToastMessage.textContent = message;
        const toast = window.bootstrap?.Toast?.getOrCreateInstance(saveToastElement);
        if (toast) {
            toast.show();
        }
    }

    function updateWattageIndicator(estimated, psuWattage, gpuRecommendedPsu) {
        wattagePill.className = "badge rounded-pill wattage-pill px-3 py-2";

        if (estimated === 0) {
            wattagePill.classList.add("wattage-neutral");
            wattagePill.textContent = "Wattage pending";
            powerHealthText.textContent = "Select components to evaluate wattage headroom.";
            return;
        }

        if (psuWattage < estimated) {
            wattagePill.classList.add("wattage-danger");
            wattagePill.textContent = "Power deficit";
            powerHealthText.textContent = `Estimated ${estimated}W exceeds PSU output (${psuWattage}W).`;
            return;
        }

        if (gpuRecommendedPsu > 0 && psuWattage < gpuRecommendedPsu) {
            wattagePill.classList.add("wattage-warning");
            wattagePill.textContent = "Low headroom";
            powerHealthText.textContent = `System can run at ${estimated}W, but GPU recommends ${gpuRecommendedPsu}W PSU.`;
            return;
        }

        wattagePill.classList.add("wattage-good");
        wattagePill.textContent = "Healthy power budget";
        powerHealthText.textContent = `Estimated draw is ${estimated}W with comfortable PSU headroom.`;
    }
})();
