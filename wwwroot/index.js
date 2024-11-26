let isMiningEnabled = false;
const interval = 3000; // Interval in milliseconds 

async function fetchBlocks() {
    const response = await fetch('/blocks');
    const data = await response.json();
    const blocksContainer = document.getElementById('blocks');
    blocksContainer.innerHTML = ''; // Clear previous blocks

    data.forEach(block => {
        const blockDiv = document.createElement('div');
        blockDiv.classList.add('block');

        // Create an h3 element for the block index
        const indexHeader = document.createElement('h3');
        indexHeader.innerText = `Block #${block.index}`;
        blockDiv.appendChild(indexHeader);

        // Create a list for block properties
        const blockList = document.createElement('ul');

        // Check if the data is a file URL and create a link if so
        const blockData = block.data;
        const isFile = blockData.startsWith("file: ");
        const dataContent = isFile ? `<a href="/getfile?publicIp=${block.signature}&fileName=${blockData.split(": ")[1]}">${blockData.replace("file: ", "")}}</a>` : blockData;

        // Create list items for each property
        const properties = [
            { label: 'Previous Hash', value: block.prevHash },
            { label: 'Timestamp', value: block.timeStamp },
            { label: 'Data', value: dataContent },  // Link if it's a file
            { label: 'Hash', value: block.hash },
            { label: 'Signature', value: block.signature },
            { label: 'Nonce', value: block.nonce }
        ];

        properties.forEach(prop => {
            const listItem = document.createElement('li');
            listItem.innerHTML = `<strong>${prop.label}:</strong> ${prop.value}`;
            blockList.appendChild(listItem);
        });

        blockDiv.appendChild(blockList); 
        blocksContainer.appendChild(blockDiv);
    });
}


async function checkAndMinePendingBlocks() {
    if (!isMiningEnabled) return; 

    const response = await fetch('/pendingBlocks');
    const pendingBlocks = await response.json();

    if (pendingBlocks.length > 0) {
        for (let block of pendingBlocks) {
            await mineBlock(block); 
        }
    }
}

async function mineBlock(block) {
    const response = await fetch('/mine', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify(block),
    });
    const result = await response.text();
    console.log(result); // Log the mining result
}

async function createBlock() {
    const blockData = document.getElementById('blockData').value;
    const fileInput = document.getElementById('fileInput').files[0];

    document.getElementById('blockData').value = '';
    document.getElementById('fileInput').value = '';

    const formData = new FormData();
    if (blockData) formData.append('blockData', blockData);
    if (fileInput) {
        formData.append('fileInput', fileInput);
        console.log(fileInput);
        formData.append('fileName', fileInput.name);
    }

    if (fileInput || blockData) {
        const response = await fetch('/create', {
            method: 'POST',
            body: formData,
        });
        const result = await response.text();
        document.getElementById('result').innerText = result;
        fetchBlocks(); // Refresh the block view after creation
    }
    else {
        alert('Please enter some data or select a file');
    }
}

function toggleMining() {
    isMiningEnabled = document.getElementById('mineCheckbox').checked;
    console.log("Mining enabled: " + isMiningEnabled);

    // If mining is enabled, start the continuous check
    if (isMiningEnabled) checkAndMinePendingBlocks(); //TODO: change it
}

async function searchBlock() {
    const data = prompt('Enter block data to search for:');
    if (data) {
        const response = await fetch(`/searchData?data=${encodeURIComponent(data)}`);
        const result = await response.json();
        document.getElementById('result').innerText = JSON.stringify(result, null, 2);
    }
}

setInterval(fetchBlocks, interval);