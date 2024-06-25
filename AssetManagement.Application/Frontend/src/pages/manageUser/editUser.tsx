import { useNavigate, useParams, useSearchParams } from "react-router-dom";
import UserForm from "../../app/components/forms/userForm";
import agent from "../../app/api/agent";
import { EditUserRequest } from "../../app/models/login/EditUserRequest";


const EditUserPage = () => {
    const { id } = useParams();
    const navigate = useNavigate();
    const [searchParams] = useSearchParams();
    const callbackUrl = searchParams.get('callbackUrl') ?? undefined; // Retrieve the callback URL
    
    if (!id) {
        navigate('/not-found');
        return null; 
    }

    const { data,  error, isLoading } = agent.Users.details(id);
    
    const onSubmit = async (formData: any) => {
        const updateData: EditUserRequest = {
            DateOfBirth: formData.dateOfBirth,
            JoinedDate: formData.joinedDate,
            Gender: formData.gender,
            Type: formData.type,
        }
        await agent.Users.update(id, updateData);
        navigate(`/manage-user?passedOrderBy=${encodeURIComponent('lastUpdate')}&passedOrderBy=${encodeURIComponent('desc')}`);
    }

    if (isLoading) {
        return <div>Loading...</div>; // Display a loading state
    }

    if (error) {
        console.error(error);
        return <div>Error loading user data</div>; // Display an error state
    }

    if (!data || !data.result) {
        console.error('Unexpected data format:', data);
        return <div>Error: Unexpected data format</div>;
    }

    return (
        <UserForm 
            onSubmit={onSubmit} 
            isEditing={true} 
            data={data.result} 
            callbackUrl={callbackUrl}
        />
    );
}

export default EditUserPage;