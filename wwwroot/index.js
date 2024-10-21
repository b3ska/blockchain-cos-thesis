let isMiningEnabled = false;
const interval = 3000; // Interval in milliseconds 

async function fetchBlocks() {
    const response = await fetch('/blocks');
    const data = await response.json();
    console.log(data);
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
        const isFile = blockData.startsWith("/files/");
        const dataContent = isFile ? `<a href="${blockData}">${blockData.slice(7)}</a>` : blockData;

        // Create list items for each property
        const properties = [
            { label: 'Previous Hash', value: block.prevHash },
            { label: 'Timestamp', value: block.timeStamp },
            { label: 'Data', value: dataContent },  // Link if it's a file
            { label: 'Hash', value: block.hash },
            { label: 'Signature', value: block.signature },
            { label: 'Public Key', value: block.publicKey },
            { label: 'Nonce', value: block.nonce }
        ];

        properties.forEach(prop => {
            const listItem = document.createElement('li');
            listItem.innerHTML = `<strong>${prop.label}:</strong> ${prop.value}`;
            blockList.appendChild(listItem);
        });

        blockDiv.appendChild(blockList); // Add the list to the block div
        blocksContainer.appendChild(blockDiv); // Finally, append the block div to the main container
    });
}


// This function checks for pending blocks and mines them if mining is enabled
async function checkAndMinePendingBlocks() {
    if (!isMiningEnabled) return; // If mining is not enabled, exit

    const response = await fetch('/pendingBlocks'); // Fetch pending blocks
    const pendingBlocks = await response.json();

    if (pendingBlocks.length > 0) {
        for (let block of pendingBlocks) {
            await mineBlock(block); // Mine each pending block
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

    const formData = new FormData();
    if (blockData) formData.append('blockData', blockData);
    if (fileInput) {
        formData.append('fileInput', fileInput);
        formData.append('fileName', fileInput.name)
    }

    const response = await fetch('/create', {
        method: 'POST',
        body: formData,
    });

    const result = await response.text();
    document.getElementById('result').innerText = result;
    fetchBlocks(); // Refresh the block view after creation
}

function toggleMining() {
    isMiningEnabled = document.getElementById('mineCheckbox').checked;
    console.log("Mining enabled: " + isMiningEnabled);

    // If mining is enabled, start the continuous check
    if (isMiningEnabled) var mining = setInterval(checkAndMinePendingBlocks, interval);
    else clearInterval(mining);
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