// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

(function () {
    const config = window.pcBuilderConfig;
    if (!config || !Array.isArray(config.components)) {
        return;
    }

    const buildForm = document.getElementById("build-form");
    const checkButton = document.getElementById("check-compatibility-btn");
    const spinner = document.getElementById("validation-spinner");
    const alertsContainer = document.getElementById("compatibility-alerts");
    const compatibilityBadge = document.getElementById("summary-compat-badge");
    const wattagePill = document.getElementById("summary-wattage-pill");
    const powerHealthText = document.getElementById("summary-power-health-text");
    const selects = Array.from(document.querySelectorAll(".component-select"));

    if (!buildForm || !checkButton || !spinner || !alertsContainer || !compatibilityBadge || !wattagePill || !powerHealthText || selects.length === 0) {
        return;
    }

    const componentsById = new Map(config.components.map(component => [String(component.id), component]));
    const formatter = new Intl.NumberFormat(undefined, {
        style: "currency",
        currency: "USD",
        minimumFractionDigits: 2
    });

    buildForm.addEventListener("submit", event => {
        event.preventDefault();
    });

    selects.forEach(select => {
        select.addEventListener("change", updateSummary);
    });

    checkButton.addEventListener("click", async () => {
        await validateBuild();
    });

    updateSummary();

    function getComponent(selectId) {
        const select = document.getElementById(selectId);
        if (!select || !select.value) {
            return null;
        }

        return componentsById.get(select.value) ?? null;
    }

    function setText(id, value) {
        const element = document.getElementById(id);
        if (element) {
            element.textContent = value;
        }
    }

    function updateSummary() {
        const cpu = getComponent("CpuId");
        const motherboard = getComponent("MotherboardId");
        const ram = getComponent("RamId");
        const gpu = getComponent("GpuId");
        const psu = getComponent("PsuId");

        setText("summary-cpu", cpu?.name ?? "-");
        setText("summary-motherboard", motherboard?.name ?? "-");
        setText("summary-ram", ram?.name ?? "-");
        setText("summary-gpu", gpu?.name ?? "-");
        setText("summary-psu", psu?.name ?? "-");

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

        const totalPrice =
            (cpu?.price ?? 0) +
            (motherboard?.price ?? 0) +
            (ram?.price ?? 0) +
            (gpu?.price ?? 0) +
            (psu?.price ?? 0);

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
    }

    async function validateBuild() {
        spinner.classList.remove("d-none");
        checkButton.disabled = true;

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
            }

            const totalPrice = Number(data.totalPrice ?? 0);
            const estimatedWattage = Number(data.estimatedWattage ?? 0);
            setText("summary-total-price", formatter.format(totalPrice));
            setText("summary-estimated-wattage", `${estimatedWattage} W`);

            const psu = getComponent("PsuId");
            const gpu = getComponent("GpuId");
            updateWattageIndicator(estimatedWattage, psu?.wattage ?? 0, gpu?.recommendedPsuWattage ?? 0);
        } catch (error) {
            console.error(error);
            renderAlerts(["Unexpected error while validating compatibility."], [], false);
        } finally {
            spinner.classList.add("d-none");
            checkButton.disabled = false;
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
